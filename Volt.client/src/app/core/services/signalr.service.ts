import { Injectable, NgZone } from '@angular/core';
import { AppConfigService } from './AppConfigService';
import { HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Observable, filter, take } from 'rxjs';

export interface AdminNotificationDto {
  adminNotificationId: number;
  title: string;
  message: string;
  orderId?: number;
  orderCode?: string;
  notificationType: number;
  isRead: boolean;
  readAt?: Date;
  readByUserId?: number;
  createdDate: Date;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  notificationConnection: HubConnection | null = null;
  isConnected: boolean = false;

  private notificationSubject = new BehaviorSubject<AdminNotificationDto | null>(null);
  public notification$: Observable<AdminNotificationDto | null> = this.notificationSubject.asObservable();

  constructor(
    private appConfigService: AppConfigService,
    private ngZone: NgZone
  ) {}

  // Start admin notification connection
  public StartNotificationConnection(accessToken: string): void {
    if (this.notificationConnection) {
      return;
    }

    // Check if config is already loaded
    if (this.appConfigService.loaded$.value) {
      this.proceedWithConnection(accessToken);
      return;
    }

    // Wait for config to load from appSettings.json
    this.appConfigService.loaded$
      .pipe(
        filter(loaded => loaded === true),
        take(1)
      )
      .subscribe(() => {
        this.proceedWithConnection(accessToken);
      });
  }

  private proceedWithConnection(accessToken: string): void {
    var config = this.appConfigService.getConfig();
    var url = config.apiBaseUrl;

    // Remove trailing slash if present
    url = (url || '').replace(/\/$/, '');

    // apiBaseUrl must be configured in assets/appSettings.json
    // For production, update appSettings.json with your production backend URL
    if (!url || url.trim() === '') {
      console.error('❌ apiBaseUrl is not configured in appSettings.json');
      return;
    }

    var hubUrl = `${url}/AdminNotificationHub`;

    this.createNotificationConnection(hubUrl, accessToken);
  }

  private createNotificationConnection(hubUrl: string, accessToken: string): void {
    var connection = new HubConnectionBuilder()
      .configureLogging(LogLevel.Information)
      .withUrl(hubUrl, {
        accessTokenFactory: () => {
          const token = localStorage.getItem('auth_token') || accessToken;
          console.log('SignalR access token factory called, token exists:', !!token);
          console.log('Token length:', token ? token.length : 0);
          if (!token) {
            console.error('❌ No access token available for SignalR connection!');
          }
          return Promise.resolve(token || '');
        },
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .build();

    // Register the event handler BEFORE starting the connection
    connection.on('NewAdminNotification', (notification: AdminNotificationDto) => {
      // Use NgZone to ensure Angular change detection runs
      this.ngZone.run(() => {
        console.log('📥 New admin notification received:', notification);
        // Emit notification to subscribers
        this.notificationSubject.next(notification);
      });
    });

    // Store connection before starting
    this.notificationConnection = connection;

    connection.start()
      .then(() => {
        this.isConnected = true;
        console.log('✅ SignalR Admin Notification Connected!');
      })
      .catch((err: any) => {
        console.error('❌ Notification SignalR connection error:', err);
        this.isConnected = false;
        this.notificationConnection = null;
      });

    connection.onreconnecting(() => {
      console.log('🔄 Notification SignalR reconnecting...');
      this.isConnected = false;
    });

    connection.onreconnected(() => {
      console.log('✅ Notification SignalR reconnected!');
      this.isConnected = true;
    });

    connection.onclose((error?: Error) => {
      console.log('🔴 Notification SignalR connection closed', error);
      this.isConnected = false;
      this.notificationConnection = null;
    });
  }

  public ListenForNotifications(): Observable<AdminNotificationDto | null> {
    return this.notification$;
  }

  public StopNotificationConnection(): void {
    if (this.notificationConnection) {
      this.notificationConnection.stop()
        .then(() => {
          console.log('Notification SignalR connection stopped');
          this.notificationConnection = null;
          this.isConnected = false;
        })
        .catch((err: any) => {
          console.error('Error stopping Notification SignalR connection:', err);
        });
    }
  }
}

