# Push Notifications Implementation Guide - Volt Project

## Overview

This document provides a comprehensive guide for the push notification implementation in the Volt project. The system uses Firebase Cloud Messaging (FCM) to send push notifications to customers for all order state changes.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Firebase Setup](#firebase-setup)
3. [Device Token Storage](#device-token-storage)
4. [API Endpoints](#api-endpoints)
5. [Order State Notifications](#order-state-notifications)
6. [Implementation Details](#implementation-details)
7. [Testing Guide](#testing-guide)
8. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

The push notification system consists of the following components:

- **Firebase Cloud Messaging (FCM)**: Google's messaging service for sending notifications
- **NotificationService**: Backend service that handles sending notifications (`Infrastructure/Services/NotificationService.cs`)
- **Device Token Storage**: Database storage for FCM device tokens in `VO_Customer` table
- **Mobile App**: Client application that registers device tokens via API

### Flow Diagram

```
Mobile App → Register Device Token → POST /api/Customer/AddDevices → Store in Database
                                                      ↓
Order State Change → Retrieve Device Tokens → NotificationService → FCM → Mobile Device
```

---

## Firebase Setup

### 1. Prerequisites

- Firebase project created in [Firebase Console](https://console.firebase.google.com/)
- Firebase Admin SDK service account JSON file
- NuGet package: `FirebaseAdmin` (version 3.0.0) - already added to `Infrastructure/Infrastructure.csproj`

### 2. Firebase Configuration

#### Server-Side Configuration

1. **Download Firebase Service Account JSON**
   - Go to Firebase Console → Project Settings → Service Accounts
   - Click "Generate New Private Key"
   - Save the JSON file as `FireBaseConfigurations.json` in the `Volt.Server` root directory (same level as `Program.cs`)

2. **Firebase Configuration Extension Method**

The Firebase configuration is handled in `Infrastructure/Configuration/FireBaseConfigurations.cs`:

```csharp
public static IServiceCollection AddFireBaseConfigurations(
    this IServiceCollection services, 
    IConfiguration configuration, 
    IWebHostEnvironment webEnvironment)
```

3. **Registration in Startup**

Firebase is automatically configured in `Volt.Server/StatupExtensions.cs`:

```csharp
builder.Services.AddFireBaseConfigurations(builder.Configuration, builder.Environment);
```

**Note**: The app will start even if `FireBaseConfigurations.json` is missing, but push notifications will not work. Check console logs for Firebase initialization status.

#### Mobile App Configuration

- **Android**: Add `google-services.json` to your Android project
- **iOS**: Add `GoogleService-Info.plist` to your iOS project
- Configure Firebase SDK in your mobile app

---

## Device Token Storage

### Database Schema

Device tokens are stored in the `VO_Customer` table:

```sql
ALTER TABLE VO_Customer
ADD AndriodDevice NVARCHAR(500) NULL,  -- FCM token for Android
    IosDevice NVARCHAR(500) NULL;       -- FCM token for iOS
```

### Domain Model

The `Customer` domain model (`Domain/Models/Customer.cs`) includes:

- `AndriodDevice` (string, nullable): Android FCM token
- `IosDevice` (string, nullable): iOS FCM token

### Domain Method

The `Customer` entity has a method to update device tokens:

```csharp
customer.AddFireBaseDevices(androidDevice, iosDevice, modifiedBy);
```

### When to Store Device Tokens

Device tokens should be stored in the following scenarios:

1. **After User Login**: When a user successfully logs in
2. **After App Installation**: When the app is first installed and Firebase token is generated
3. **Token Refresh**: When Firebase refreshes the device token (tokens can expire)
4. **App Launch**: On app startup to ensure the latest token is stored

---

## API Endpoints

### Store Device Tokens for Customer

**Endpoint**: `POST /api/Customer/AddDevices`

**Authentication**: Required (User must be logged in)

**Request Body**:
```json
{
  "andriodDevice": "fcm_token_android_here",
  "iosDevice": "fcm_token_ios_here"
}
```

**Response**: 
- `200 OK`: Success
- `400 Bad Request`: Error details

**Implementation**:
- **Command**: `Application/Features/Customer/Command/SaveFireBaseTokensForCustomerCommand/SaveFireBaseTokensForCustomerCommand.cs`
- **Controller**: `Volt.Server/Controllers/CustomerController.cs` - `AddDevices` method
- **DTO**: `Application/Features/Customer/DTOs/DeliveryDeviceTokensDto.cs`

**Example Request**:
```http
POST /api/Customer/AddDevices
Authorization: Bearer {token}
Content-Type: application/json

{
  "andriodDevice": "dK3jF8hL9mN2pQ5rT7vW0xY3zA6bC8eF1gH4iJ7kL0mN3pQ6rS9tU2vW5xY8z",
  "iosDevice": null
}
```

---

## Order State Notifications

The system sends push notifications for all order state changes:

### Order States

1. **Pending** (0): Initial state when customer creates order
2. **Confirmed** (1): Admin assigns vehicles and confirms order
3. **OnWay** (2): Admin marks vehicle as dispatched
4. **CustomerReceived** (3): Admin marks that customer received vehicle
5. **Completed** (4): Admin confirms vehicle returned successfully
6. **Cancelled**: Order is cancelled (not a state, handled separately)

### Notification Types

Defined in `Domain/Enums/NotificationType.cs`:

- `OrderCreated = 1`: When order is created (Pending state)
- `OrderConfirmed = 2`: When order is confirmed
- `OrderOnWay = 3`: When order is on the way
- `OrderCustomerReceived = 4`: When customer receives the order
- `OrderCompleted = 5`: When order is completed
- `OrderCancelled = 6`: When order is cancelled

### Notification Implementation

#### 1. Order Created (Pending State)

**Location**: `Application/Features/Order/Command/CreateOrderCommand/CreateOrderCommand.cs`

**Trigger**: After order is successfully created and saved

**Notification**:
- **Title**: "Order Created"
- **Body**: "Your order #{OrderCode} has been created successfully and is pending confirmation."

#### 2. Order Confirmed

**Location**: `Application/Features/Order/Command/UpdateOrderStateCommand/UpdateOrderStateCommand.cs`

**Trigger**: When order state changes to `Confirmed`

**Notification**:
- **Title**: "Order Confirmed"
- **Body**: "Your order #{OrderCode} has been confirmed. Vehicles have been assigned."

#### 3. Order On The Way

**Location**: `Application/Features/Order/Command/UpdateOrderStateCommand/UpdateOrderStateCommand.cs`

**Trigger**: When order state changes to `OnWay`

**Notification**:
- **Title**: "Order On The Way"
- **Body**: "Your order #{OrderCode} is on the way to your location."

#### 4. Order Customer Received

**Location**: `Application/Features/Order/Command/UpdateOrderStateCommand/UpdateOrderStateCommand.cs`

**Trigger**: When order state changes to `CustomerReceived`

**Notification**:
- **Title**: "Order Received"
- **Body**: "Your order #{OrderCode} has been delivered. Please confirm receipt."

#### 5. Order Completed

**Location**: `Application/Features/Order/Command/UpdateOrderStateCommand/UpdateOrderStateCommand.cs`

**Trigger**: When order state changes to `Completed`

**Notification**:
- **Title**: "Order Completed"
- **Body**: "Your order #{OrderCode} has been completed successfully. Thank you!"

#### 6. Order Cancelled

**Location**: `Application/Features/Order/Command/CancelOrderCommand/CancelOrderCommand.cs`

**Trigger**: When order is cancelled (by customer or admin)

**Notification**:
- **Title**: "Order Cancelled"
- **Body**: "Your order #{OrderCode} has been cancelled."

### Notification Payload Structure

All notifications include the following payload:

```csharp
{
    "orderId": "123",
    "orderCode": "ORD-20260111-ABC123",
    "type": "1",  // NotificationType enum value
    "action": "open_order_detail"
}
```

The mobile app should handle the `action` field to navigate to the appropriate screen when the notification is tapped.

---

## Implementation Details

### Notification Service

**Interface**: `Infrastructure/Services/INotificationService.cs`
**Implementation**: `Infrastructure/Services/NotificationService.cs`

**Methods**:
- `SendNotificationForSingleDevice(NotificationBody notificationBody)`: Send to a single device
- `SendNotificationAsyncToMultipleDevices(NotificationBodyForMultipleDevices notificationBody)`: Send to multiple devices (up to 500)

**Registration**: Registered in `Infrastructure/DatabaseConfiguration.cs`:

```csharp
services.AddScoped<INotificationService, NotificationService>();
```

### Error Handling

- Notification failures are caught and logged but **do not affect** the main business logic
- If notification sending fails, the order operation still succeeds
- Errors are logged using `ILogger<NotificationService>`

### Android Configuration

Notifications are configured with:
- **ChannelId**: "1" (must match mobile app configuration)
- **ClickAction**: "FLUTTER_NOTIFICATION_CLICK" (for Flutter apps)
- **Priority**: HIGH
- **DefaultSound**: true

---

## Testing Guide

### Prerequisites

1. **Firebase Setup**:
   - Create Firebase project
   - Download `FireBaseConfigurations.json` and place in `Volt.Server` root
   - Configure mobile app with Firebase

2. **Database Migration**:
   - Run migration to add `AndriodDevice` and `IosDevice` columns to `VO_Customer` table

3. **Mobile App Setup**:
   - Configure Firebase in mobile app
   - Implement token registration on login/app launch

### Testing Steps

#### 1. Test Device Token Registration

**Step 1**: Login as a customer
```http
POST /api/Customer/Login
{
  "mobileNumber": "1234567890",
  "password": "password123"
}
```

**Step 2**: Register device token
```http
POST /api/Customer/AddDevices
Authorization: Bearer {token}
{
  "andriodDevice": "test_fcm_token_here",
  "iosDevice": null
}
```

**Expected**: `200 OK` response

**Verify**: Check database - `VO_Customer` table should have the token in `AndriodDevice` or `IosDevice` column

#### 2. Test Order Created Notification

**Step 1**: Create an order
```http
POST /api/CustomerOrder/CreateOrder
Authorization: Bearer {token}
{
  "subCategoryId": 1,
  "cityId": 1,
  "reservationDateFrom": "2026-01-15T00:00:00Z",
  "reservationDateTo": "2026-01-17T00:00:00Z",
  "vehiclesCount": 1,
  "paymentMethodId": 0,
  "mobileTotal": 100.00,
  ...
}
```

**Expected**: 
- Order created successfully
- Notification sent to customer's device
- Check mobile device for notification with title "Order Created"

#### 3. Test Order State Change Notifications

**Step 1**: Confirm order (Admin)
```http
PUT /api/AdminOrder/UpdateOrderState
Authorization: Bearer {admin_token}
{
  "orderId": 1,
  "newState": 1,  // Confirmed
  "vehicleIds": [1, 2]
}
```

**Expected**: Notification "Order Confirmed" sent

**Step 2**: Mark order as OnWay
```http
PUT /api/AdminOrder/UpdateOrderState
{
  "orderId": 1,
  "newState": 2  // OnWay
}
```

**Expected**: Notification "Order On The Way" sent

**Step 3**: Mark customer received
```http
PUT /api/AdminOrder/UpdateOrderState
{
  "orderId": 1,
  "newState": 3  // CustomerReceived
}
```

**Expected**: Notification "Order Received" sent

**Step 4**: Complete order
```http
PUT /api/AdminOrder/UpdateOrderState
{
  "orderId": 1,
  "newState": 4  // Completed
}
```

**Expected**: Notification "Order Completed" sent

#### 4. Test Order Cancellation Notification

**Step 1**: Cancel order
```http
POST /api/CustomerOrder/CancelOrder
Authorization: Bearer {token}
{
  "orderId": 1
}
```

**Expected**: Notification "Order Cancelled" sent

### Testing Without Real Devices

If you don't have a real device, you can:

1. **Check Logs**: Notification service logs all operations
2. **Check Database**: Verify device tokens are stored
3. **Use Firebase Console**: Send test notifications from Firebase Console
4. **Mock Service**: Temporarily replace `NotificationService` with a mock that logs notifications

### Testing Checklist

- [ ] Device token registration works
- [ ] Order created notification sent
- [ ] Order confirmed notification sent
- [ ] Order on way notification sent
- [ ] Order customer received notification sent
- [ ] Order completed notification sent
- [ ] Order cancelled notification sent
- [ ] Notifications appear on mobile device
- [ ] Notification tap opens correct screen
- [ ] Multiple devices receive notifications
- [ ] Invalid tokens are handled gracefully

---

## Troubleshooting

### Common Issues

#### 1. Notifications Not Received

**Possible Causes**:
- Firebase configuration file missing or incorrect
- Device token not registered in database
- Invalid or expired device token
- Firebase service account permissions incorrect

**Solutions**:
1. Check if `FireBaseConfigurations.json` exists in `Volt.Server` root
2. Verify device token is stored in database:
   ```sql
   SELECT CustomerId, AndriodDevice, IosDevice FROM VO_Customer WHERE CustomerId = {customerId}
   ```
3. Check application logs for Firebase errors
4. Verify Firebase service account has "Firebase Cloud Messaging API" enabled

#### 2. Firebase Initialization Errors

**Error**: "FirebaseApp already exists"

**Solution**: This is normal if the app restarts. Firebase handles this automatically.

**Error**: "Failed to initialize Firebase"

**Solution**: 
- Check `FireBaseConfigurations.json` file path
- Verify JSON file is valid
- Check file permissions

#### 3. Invalid Token Errors

**Error**: "Invalid token" in logs

**Solution**: 
- Token may have expired - mobile app should refresh and re-register
- User may have uninstalled the app
- Token may be from a different Firebase project

#### 4. Notifications Delayed

**Possible Causes**:
- FCM service status issues
- Network connectivity problems
- Device battery optimization settings

**Solutions**:
1. Check [FCM Status Page](https://status.firebase.google.com/)
2. Verify device has internet connection
3. Check device notification settings
4. Disable battery optimization for the app

#### 5. Android Notifications Not Showing

**Possible Causes**:
- Notification channel not created in mobile app
- App notification permissions not granted
- ChannelId mismatch

**Solutions**:
1. Verify notification channel "1" is created in mobile app
2. Check app notification permissions
3. Ensure `ChannelId` in `NotificationService.cs` matches mobile app

### Debugging Tips

1. **Enable Detailed Logging**: Check `appsettings.json` for log levels
2. **Check Firebase Console**: View notification delivery status
3. **Test with Single Device**: Start with one device token
4. **Verify Payload**: Check notification payload structure matches mobile app expectations
5. **Check Network**: Ensure server can reach FCM servers

### Logs to Monitor

- Firebase initialization: "Firebase initialized successfully" or warning messages
- Notification sending: "Firebase success count: X, Firebase failed count: Y"
- Token errors: "Failed to send notification to token {Token}: {Error}"

---

## Database Migration

To add device token columns to the database, create a migration:

```bash
dotnet ef migrations add AddDeviceTokensToCustomer --project Infrastructure --startup-project Volt.Server
```

Then apply the migration:

```bash
dotnet ef database update --project Infrastructure --startup-project Volt.Server
```

Or the migration will be applied automatically on app startup (see `Program.cs`).

---

## Best Practices

1. **Token Management**:
   - Always validate tokens before sending
   - Handle token expiration gracefully
   - Remove invalid tokens from database when detected

2. **Error Handling**:
   - Never let notification failures affect business logic
   - Log all notification errors for monitoring
   - Implement retry logic for transient failures (future enhancement)

3. **Performance**:
   - Use `SendEachForMulticastAsync` for multiple devices (max 500 per batch)
   - Send notifications asynchronously (already implemented)
   - Don't block order operations waiting for notifications

4. **Security**:
   - Never commit `FireBaseConfigurations.json` to version control
   - Use environment variables or secure storage for production
   - Validate user authentication before storing tokens

5. **Mobile App**:
   - Request notification permissions on first launch
   - Register tokens after login
   - Refresh tokens when they change
   - Handle notification taps appropriately

---

## Summary

This implementation provides a complete push notification system for the Volt project:

1. **Device tokens are stored** in `VO_Customer` table (`AndriodDevice`, `IosDevice` columns)
2. **API endpoint** (`POST /api/Customer/AddDevices`) handles token registration
3. **NotificationService** sends notifications using Firebase Cloud Messaging
4. **Notifications are sent** for all order state changes:
   - Order Created (Pending)
   - Order Confirmed
   - Order On The Way
   - Order Customer Received
   - Order Completed
   - Order Cancelled
5. **Both single and multiple device** notifications are supported

For questions or issues, refer to the [Firebase Cloud Messaging Documentation](https://firebase.google.com/docs/cloud-messaging).

---

## Files Modified/Created

### Domain Layer
- `Domain/Models/Customer.cs` - Added device token properties and `AddFireBaseDevices` method
- `Domain/Enums/NotificationType.cs` - Created enum for notification types

### Infrastructure Layer
- `Infrastructure/Services/INotificationService.cs` - Created notification service interface
- `Infrastructure/Services/NotificationService.cs` - Created notification service implementation
- `Infrastructure/Configuration/FireBaseConfigurations.cs` - Created Firebase configuration extension
- `Infrastructure/MappingConfiguration/CustomerConfiguration.cs` - Added device token column mappings
- `Infrastructure/DatabaseConfiguration.cs` - Registered notification service
- `Infrastructure/Infrastructure.csproj` - Added FirebaseAdmin NuGet package

### Application Layer
- `Application/Features/Customer/Command/SaveFireBaseTokensForCustomerCommand/SaveFireBaseTokensForCustomerCommand.cs` - Created command to save tokens
- `Application/Features/Customer/DTOs/DeliveryDeviceTokensDto.cs` - Created DTO for device tokens
- `Application/Features/Order/Command/CreateOrderCommand/CreateOrderCommand.cs` - Added notification for order creation
- `Application/Features/Order/Command/UpdateOrderStateCommand/UpdateOrderStateCommand.cs` - Added notifications for all state changes
- `Application/Features/Order/Command/CancelOrderCommand/CancelOrderCommand.cs` - Added notification for order cancellation

### Presentation Layer
- `Volt.Server/Controllers/CustomerController.cs` - Added `AddDevices` endpoint
- `Volt.Server/StatupExtensions.cs` - Added Firebase configuration

---

## Next Steps

1. **Create Database Migration**: Add device token columns to database
2. **Configure Firebase**: Download and place `FireBaseConfigurations.json`
3. **Test Implementation**: Follow testing guide above
4. **Mobile App Integration**: Implement token registration in mobile app
5. **Monitor**: Set up logging and monitoring for notification delivery

---

**Last Updated**: January 2026
**Version**: 1.0


