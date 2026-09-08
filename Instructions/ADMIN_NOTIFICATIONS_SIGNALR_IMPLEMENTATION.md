# SignalR Admin Notifications Implementation Guide - Volt Project

## Overview

This document provides a comprehensive guide for implementing real-time notifications to admin users using SignalR in the Volt project. The system enables instant notification delivery to connected web clients (admin panel) when order-related events occur, such as order creation or state changes.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [SignalR Setup](#signalr-setup)
3. [Server-Side Implementation](#server-side-implementation)
4. [Client-Side Implementation](#client-side-implementation)
5. [Notification Flow](#notification-flow)
6. [When to Send Notifications](#when-to-send-notifications)
7. [API Endpoints](#api-endpoints)
8. [Database Schema](#database-schema)
9. [Implementation Examples](#implementation-examples)
10. [Best Practices](#best-practices)
11. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

The SignalR notification system consists of:

- **AdminNotificationHub**: SignalR Hub that manages connections and broadcasts notifications
- **AdminNotificationHubService**: Service layer that creates notifications and sends them via SignalR
- **AdminNotification Domain Model**: Entity that persists notifications to the database
- **Client SignalR Service**: Angular service that manages SignalR connection
- **Admin UI Components**: Components that display and handle notifications

### Flow Diagram

```
Order Event → Command Handler → AdminNotificationHubService → Create Notification in DB
                                                              ↓
                                                      SignalR Hub → Broadcast to Admin Clients
                                                              ↓
                                                    Angular SignalR Service → Notification Component
```

---

## SignalR Setup

### 1. Prerequisites

- **Server**: ASP.NET Core 8.0 with SignalR package
- **Client**: Angular with `@microsoft/signalr` package
- **Authentication**: JWT-based authentication for SignalR connections

### 2. Server-Side Setup

#### Install NuGet Package

The SignalR package is already added to `Presentation/Presentation.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
```

#### Configure SignalR in StatupExtensions.cs

```csharp
// Add SignalR service
builder.Services.AddSignalR();

// Map SignalR Hub
app.MapHub<Presentation.Hubs.AdminNotificationHub>("/AdminNotificationHub");
```

#### Register AdminNotificationHubService

The service is registered in `Infrastructure/DatabaseConfiguration.cs`:

```csharp
services.AddScoped<Infrastructure.Services.IAdminNotificationHubService, Presentation.Services.AdminNotificationHubService>();
```

**Note**: The interface `IAdminNotificationHubService` is defined in `Infrastructure/Services/` to maintain clean architecture (Application layer can reference Infrastructure, but not Presentation).

### 3. Client-Side Setup

#### Install NPM Package

```bash
npm install @microsoft/signalr
```

#### Package.json

```json
{
  "dependencies": {
    "@microsoft/signalr": "^8.0.0"
  }
}
```

---

## Server-Side Implementation

### 1. SignalR Hub

The hub manages client connections and handles connection/disconnection events.

**File**: `Presentation/Hubs/AdminNotificationHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class AdminNotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            System.Console.WriteLine($"[AdminNotificationHub] Client connected: {Context.ConnectionId}");
            System.Console.WriteLine($"[AdminNotificationHub] User: {Context.UserIdentifier}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            System.Console.WriteLine($"[AdminNotificationHub] Client disconnected: {Context.ConnectionId}");
            if (exception != null)
            {
                System.Console.WriteLine($"[AdminNotificationHub] Disconnect error: {exception.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
```

**Key Points**:
- `OnConnectedAsync`: Called when a client connects
- `OnDisconnectedAsync`: Called when a client disconnects
- `Context.UserIdentifier`: Contains the user ID from JWT token (if configured)
- `Context.ConnectionId`: Unique connection identifier

### 2. AdminNotificationHubService

This service creates notifications in the database and sends them via SignalR.

**File**: `Presentation/Services/AdminNotificationHubService.cs`

```csharp
public class AdminNotificationHubService : IAdminNotificationHubService
{
    private readonly IHubContext<AdminNotificationHub> _hubContext;
    private readonly IMediator _mediator;

    public async Task SendNotificationAsync(
        string title,
        string message,
        NotificationType notificationType,
        int? orderId = null)
    {
        // Step 1: Create notification in database
        var createCommand = new CreateAdminNotificationCommand { ... };
        var result = await _mediator.Send(createCommand);

        // Step 2: Prepare notification DTO for SignalR
        var notificationDto = new { ... };

        // Step 3: Send notification via SignalR to all connected admin clients
        await _hubContext.Clients.All.SendAsync("NewAdminNotification", notificationDto);
    }
}
```

**Key Points**:
- Creates notification in database first (for persistence)
- Sends notification via SignalR to all connected admin clients
- Uses `NewAdminNotification` as the method name (client listens for this)

### 3. AdminNotification Domain Model

**File**: `Domain/Models/AdminNotification.cs`

```csharp
public class AdminNotification : IAuditable
{
    public int AdminNotificationId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public int? OrderId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public int? ReadByUserId { get; private set; }
    // ... audit properties
}
```

### 4. NotificationType Enum

Already defined in `Domain/Enums/NotificationType.cs`:

```csharp
public enum NotificationType
{
    OrderCreated = 1,
    OrderConfirmed = 2,
    OrderOnWay = 3,
    OrderCustomerReceived = 4,
    OrderCompleted = 5,
    OrderCancelled = 6
}
```

---

## Client-Side Implementation

### 1. SignalR Service

Angular service that manages SignalR connection and handles incoming notifications.

**File**: `Volt.client/src/app/shared/services/signalr.service.ts`

```typescript
import { Injectable, NgZone } from '@angular/core';
import { HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private notificationConnection: HubConnection | null = null;
  isConnected: boolean = false;
  
  private notificationSubject = new BehaviorSubject<any>(null);
  public notification$: Observable<any> = this.notificationSubject.asObservable();

  constructor(private ngZone: NgZone) {}

  // Start notification connection
  StartAdminNotificationConnection(accessToken: string): void {
    const hubUrl = `${this.getBaseUrl()}/AdminNotificationHub`;
    this.createNotificationConnection(hubUrl, accessToken);
  }

  private getBaseUrl(): string {
    // Return your API base URL
    return 'https://your-api-url.com';
  }

  private createNotificationConnection(hubUrl: string, accessToken: string): void {
    var connection = new HubConnectionBuilder()
      .configureLogging(LogLevel.Information)
      .withUrl(hubUrl, {
        accessTokenFactory: () => {
          const token = localStorage.getItem('accessToken') || accessToken;
          return Promise.resolve(token || '');
        },
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .build();

    // Register event handler BEFORE starting connection
    connection.on('NewAdminNotification', (notification: any) => {
      this.ngZone.run(() => {
        this.notificationSubject.next(notification);
      });
    });

    this.notificationConnection = connection;

    // Start connection
    connection.start().then(() => {
      this.isConnected = true;
      console.log('✅ SignalR Connected!');
    }).catch((err) => {
      console.error('❌ Notification SignalR connection error:', err);
      this.isConnected = false;
    });
  }

  // Stop connection
  async StopAdminNotificationConnection(): Promise<void> {
    if (this.notificationConnection) {
      await this.notificationConnection.stop();
      this.notificationConnection = null;
      this.isConnected = false;
    }
  }
}
```

### 2. Notification Service (Angular)

Service that manages notification state and API calls.

**File**: `Volt.client/src/app/shared/services/admin-notification.service.ts`

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { SignalRService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class AdminNotificationService {
  private notificationsSubject = new BehaviorSubject<any[]>([]);
  public notifications$: Observable<any[]> = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$: Observable<number> = this.unreadCountSubject.asObservable();

  constructor(
    private http: HttpClient,
    private signalRService: SignalRService
  ) {
    // Subscribe to SignalR notifications
    this.signalRService.notification$.subscribe(notification => {
      if (notification) {
        this.addNotification(notification);
      }
    });
  }

  // Load notifications from API
  loadNotifications(isRead?: boolean, skip?: number, take?: number): Observable<any> {
    let url = '/api/AdminNotification';
    const params: any = {};
    if (isRead !== undefined) params.isRead = isRead;
    if (skip !== undefined) params.skip = skip;
    if (take !== undefined) params.take = take;
    
    return this.http.get(url, { params });
  }

  // Get unread count
  getUnreadCount(): Observable<number> {
    return this.http.get<number>('/api/AdminNotification/UnreadCount');
  }

  // Mark as read
  markAsRead(id: number): Observable<any> {
    return this.http.post(`/api/AdminNotification/${id}/MarkAsRead`, {});
  }

  // Add notification to list
  addNotification(notification: any): void {
    const current = this.notificationsSubject.value;
    const updated = [notification, ...current];
    this.notificationsSubject.next(updated);
    
    if (!notification.isRead) {
      this.updateUnreadCount();
    }
  }

  // Update unread count
  updateUnreadCount(): void {
    this.getUnreadCount().subscribe(count => {
      this.unreadCountSubject.next(count);
    });
  }
}
```

### 3. Notification Component

Component that displays notifications in the admin panel.

**File**: `Volt.client/src/app/pages/admin/notifications/notifications.component.ts`

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { SignalRService } from 'src/app/shared/services/signalr.service';
import { AdminNotificationService } from 'src/app/shared/services/admin-notification.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
  selector: 'app-admin-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class AdminNotificationsComponent implements OnInit, OnDestroy {
  notifications: any[] = [];
  unreadCount: number = 0;
  isOpen = false;
  private subscriptions = new Subscription();

  constructor(
    private notificationService: AdminNotificationService,
    private signalRService: SignalRService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Load initial notifications
    this.loadNotifications();
    this.loadUnreadCount();

    // Subscribe to notifications
    this.subscriptions.add(
      this.notificationService.notifications$.subscribe(notifications => {
        this.notifications = notifications;
      })
    );

    // Subscribe to unread count
    this.subscriptions.add(
      this.notificationService.unreadCount$.subscribe(count => {
        this.unreadCount = count;
      })
    );

    // Start SignalR connection
    const token = this.authService.getAccessToken();
    if (token) {
      this.signalRService.StartAdminNotificationConnection(token);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.signalRService.StopAdminNotificationConnection();
  }

  loadNotifications(): void {
    this.notificationService.loadNotifications(false, 0, 50).subscribe(result => {
      this.notificationService.notificationsSubject.next(result);
    });
  }

  loadUnreadCount(): void {
    this.notificationService.updateUnreadCount();
  }

  markAsRead(id: number): void {
    this.notificationService.markAsRead(id).subscribe(() => {
      this.loadNotifications();
      this.loadUnreadCount();
    });
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
  }
}
```

---

## Notification Flow

### Complete Flow Example

1. **Order Created**: Customer creates a new order
2. **Backend Event**: `CreateOrderCommand` handler processes the order
3. **AdminNotificationHubService**: Creates notification and sends via SignalR
4. **SignalR Hub**: Broadcasts to all connected admin clients
5. **Client SignalR Service**: Receives notification
6. **Notification Service**: Adds notification to state
7. **Notification Component**: Displays notification in UI

---

## When to Send Notifications

Notifications are sent in the following scenarios:

### 1. Order Created

**Location**: `Application/Features/Order/Command/CreateOrderCommand/CreateOrderCommand.cs`

```csharp
// Send admin notification via SignalR
await _adminNotificationHubService.SendNotificationAsync(
    title: "New Order Created",
    message: $"New order #{order.OrderCode} has been created by customer {customer.FullName}",
    notificationType: Domain.Enums.NotificationType.OrderCreated,
    orderId: order.OrderId
);
```

### 2. Order State Changed

**Location**: `Application/Features/Order/Command/UpdateOrderStateCommand/UpdateOrderStateCommand.cs`

Notifications are sent for:
- **Order Confirmed**: When order is confirmed and vehicles assigned
- **Order On The Way**: When order is dispatched
- **Order Customer Received**: When customer receives the order
- **Order Completed**: When order is completed

### 3. Order Cancelled

**Location**: `Application/Features/Order/Command/CancelOrderCommand/CancelOrderCommand.cs`

```csharp
await _adminNotificationHubService.SendNotificationAsync(
    title: "Order Cancelled",
    message: $"Order #{order.OrderCode} has been cancelled",
    notificationType: Domain.Enums.NotificationType.OrderCancelled,
    orderId: order.OrderId
);
```

---

## API Endpoints

### Get Notifications

**Endpoint**: `GET /api/AdminNotification`

**Query Parameters**:
- `isRead` (optional): Filter by read status (true/false)
- `skip` (optional): Number of records to skip (pagination)
- `take` (optional): Number of records to take (pagination)

**Response**: List of `AdminNotificationDto`

**Example**:
```http
GET /api/AdminNotification?isRead=false&skip=0&take=50
Authorization: Bearer {token}
```

### Get Unread Count

**Endpoint**: `GET /api/AdminNotification/UnreadCount`

**Response**: `int` - Count of unread notifications

**Example**:
```http
GET /api/AdminNotification/UnreadCount
Authorization: Bearer {token}
```

### Mark Notification as Read

**Endpoint**: `POST /api/AdminNotification/{id}/MarkAsRead`

**Response**: `200 OK`

**Example**:
```http
POST /api/AdminNotification/1/MarkAsRead
Authorization: Bearer {token}
```

---

## Database Schema

### AdminNotification Table

**Table Name**: `VO_AdminNotification`

**Columns**:
- `AdminNotificationId` (int, PK, Identity)
- `Title` (nvarchar(500), Required)
- `Message` (nvarchar(2000), Required)
- `OrderId` (int, Nullable, FK to VO_Order)
- `NotificationType` (int, Required)
- `IsRead` (bit, Required, Default: false)
- `ReadAt` (datetime2, Nullable)
- `ReadByUserId` (int, Nullable, FK to AspNetUsers)
- `CreatedBy` (nvarchar(256), Nullable)
- `CreatedDate` (datetime2, Required)
- `LastModifiedBy` (nvarchar(256), Nullable)
- `LastModifiedDate` (datetime2, Required)

**Indexes**:
- `OrderId`
- `IsRead`
- `CreatedDate`
- `NotificationType`

### Migration

To create the database table, run:

```bash
dotnet ef migrations add AddAdminNotificationTable --project Infrastructure --startup-project Volt.Server
dotnet ef database update --project Infrastructure --startup-project Volt.Server
```

---

## Implementation Examples

### Example 1: Order Created Notification

**Backend**:
```csharp
// In CreateOrderCommand handler
await _adminNotificationHubService.SendNotificationAsync(
    title: "New Order Created",
    message: $"New order #{order.OrderCode} has been created by customer {customer.FullName}",
    notificationType: NotificationType.OrderCreated,
    orderId: order.OrderId
);
```

**Client**:
```typescript
// Notification will be received automatically via SignalR
// Display in notification dropdown
```

### Example 2: Order State Change Notification

```csharp
// In UpdateOrderStateCommand handler
await _adminNotificationHubService.SendNotificationAsync(
    title: "Order Confirmed",
    message: $"Order #{order.OrderCode} has been confirmed and vehicles assigned",
    notificationType: NotificationType.OrderConfirmed,
    orderId: order.OrderId
);
```

---

## Best Practices

### 1. Connection Management

- **Start connection after login**: Only connect when admin user is authenticated
- **Stop connection on logout**: Clean up connections when user logs out
- **Handle reconnection**: Implement automatic reconnection with exponential backoff
- **Monitor connection state**: Track connection status for debugging

### 2. Error Handling

- Never let notification failures affect business logic
- Log all notification errors for monitoring
- Implement retry logic for transient failures

### 3. Authentication

- **Use JWT tokens**: Pass access token in SignalR connection
- **Validate on server**: Ensure user is authenticated before accepting connection
- **Authorize endpoints**: Use `[Authorize(Roles = "Admin")]` on notification endpoints

### 4. Performance

- **Limit notification history**: Don't load all notifications at once
- **Pagination**: Implement pagination for notification list
- **Debounce updates**: Debounce rapid notification updates
- **Clean up subscriptions**: Unsubscribe from observables on component destroy

### 5. User Experience

- **Show connection status**: Display connection indicator in UI
- **Handle offline state**: Show notifications when connection is restored
- **Mark as read**: Implement read/unread functionality
- **Badge count**: Show unread count badge
- **Auto-refresh**: Periodically refresh unread count

### 6. Security

- **Authorize connections**: Only allow authenticated admin users to connect
- **Validate user identity**: Ensure users can only receive admin notifications
- **Sanitize data**: Sanitize notification content before sending
- **Rate limiting**: Implement rate limiting for notification sending

---

## Troubleshooting

### Common Issues

#### 1. Connection Not Establishing

**Symptoms**: SignalR connection fails to start

**Solutions**:
- Verify SignalR hub is mapped in `StatupExtensions.cs`
- Check CORS configuration allows SignalR endpoints
- Verify JWT token is valid and included
- Check network connectivity
- Verify WebSocket support on server

#### 2. Notifications Not Received

**Symptoms**: Notifications sent but not received on client

**Solutions**:
- Verify `NewAdminNotification` event handler is registered
- Check connection is established (`isConnected === true`)
- Verify notification is being sent to correct clients
- Check browser console for errors
- Verify NgZone is used for change detection

#### 3. Connection Drops Frequently

**Symptoms**: Connection disconnects and reconnects repeatedly

**Solutions**:
- Check network stability
- Increase reconnection intervals
- Verify server timeout settings
- Check for proxy/firewall issues
- Monitor server logs for errors

#### 4. Authentication Failures

**Symptoms**: Connection rejected due to authentication

**Solutions**:
- Verify JWT token is valid
- Check token expiration
- Verify `accessTokenFactory` returns correct token
- Check server authentication configuration
- Verify user has Admin role

---

## Summary

This implementation provides a complete real-time notification system for admin users:

1. **Server-side**: SignalR Hub and AdminNotificationHubService handle connection and broadcasting
2. **Client-side**: Angular SignalR service manages connection and receives notifications
3. **Notifications are sent** when order events occur (order creation, state changes, cancellation)
4. **Notifications are persisted** in the database for history
5. **Real-time delivery** ensures admins see notifications immediately
6. **Automatic reconnection** handles connection failures gracefully
7. **API endpoints** provide access to notification history and management

For questions or issues, refer to the [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction).

---

## Files Created/Modified

### Domain Layer
- `Domain/Models/AdminNotification.cs` - Created admin notification entity

### Infrastructure Layer
- `Infrastructure/MappingConfiguration/AdminNotificationConfiguration.cs` - Created entity configuration
- `Infrastructure/DatabaseContext.cs` - Added AdminNotifications DbSet
- `Infrastructure/DatabaseConfiguration.cs` - Registered AdminNotificationHubService

### Application Layer
- `Application/Features/AdminNotification/Command/CreateAdminNotificationCommand/` - Created command
- `Application/Features/AdminNotification/Command/MarkAdminNotificationAsReadCommand/` - Created command
- `Application/Features/AdminNotification/Query/GetAdminNotificationsQuery/` - Created query
- `Application/Features/AdminNotification/Query/GetUnreadAdminNotificationsCountQuery/` - Created query
- `Application/Features/AdminNotification/DTOs/AdminNotificationDto.cs` - Created DTO
- `Application/Features/Order/Command/CreateOrderCommand/` - Updated to send admin notification
- `Application/Features/Order/Command/UpdateOrderStateCommand/` - Updated to send admin notification
- `Application/Features/Order/Command/CancelOrderCommand/` - Updated to send admin notification

### Presentation Layer
- `Presentation/Hubs/AdminNotificationHub.cs` - Created SignalR hub
- `Presentation/Services/AdminNotificationHubService.cs` - Created hub service
- `Presentation/Presentation.csproj` - Added SignalR package

### Server Layer
- `Volt.Server/StatupExtensions.cs` - Configured SignalR
- `Volt.Server/Controllers/AdminNotificationController.cs` - Created controller

---

