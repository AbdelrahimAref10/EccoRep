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

export interface MerchantNotificationDto {
  merchantNotificationId: number;
  merchantId: number;
  title: string;
  message: string;
  orderId?: number;
  orderCode?: string;
  notificationType: number;
  isRead: boolean;
  readAt?: Date;
  createdDate: Date;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  notificationConnection: HubConnection | null = null;
  merchantNotificationConnection: HubConnection | null = null;
  isConnected = false;
  isMerchantConnected = false;

  private notificationSubject = new BehaviorSubject<AdminNotificationDto | null>(null);
  public notification$: Observable<AdminNotificationDto | null> = this.notificationSubject.asObservable();

  private merchantNotificationSubject = new BehaviorSubject<MerchantNotificationDto | null>(null);
  public merchantNotification$: Observable<MerchantNotificationDto | null> =
    this.merchantNotificationSubject.asObservable();

  constructor(
    private appConfigService: AppConfigService,
    private ngZone: NgZone
  ) {}

  public StartNotificationConnection(accessToken: string): void {
    if (this.notificationConnection) {
      return;
    }

    if (this.appConfigService.loaded$.value) {
      this.proceedWithAdminConnection(accessToken);
      return;
    }

    this.appConfigService.loaded$
      .pipe(
        filter(loaded => loaded === true),
        take(1)
      )
      .subscribe(() => this.proceedWithAdminConnection(accessToken));
  }

  public StartMerchantNotificationConnection(accessToken: string): void {
    if (this.merchantNotificationConnection) {
      return;
    }

    if (this.appConfigService.loaded$.value) {
      this.proceedWithMerchantConnection(accessToken);
      return;
    }

    this.appConfigService.loaded$
      .pipe(
        filter(loaded => loaded === true),
        take(1)
      )
      .subscribe(() => this.proceedWithMerchantConnection(accessToken));
  }

  private proceedWithAdminConnection(accessToken: string): void {
    const url = this.resolveBaseUrl();
    if (!url) {
      return;
    }
    this.createAdminConnection(`${url}/AdminNotificationHub`, accessToken);
  }

  private proceedWithMerchantConnection(accessToken: string): void {
    const url = this.resolveBaseUrl();
    if (!url) {
      return;
    }
    this.createMerchantConnection(`${url}/MerchantNotificationHub`, accessToken);
  }

  private resolveBaseUrl(): string | null {
    const config = this.appConfigService.getConfig();
    const url = (config.apiBaseUrl || '').replace(/\/$/, '');
    if (!url || url.trim() === '') {
      console.error('❌ apiBaseUrl is not configured in appSettings.json');
      return null;
    }
    return url;
  }

  private createAdminConnection(hubUrl: string, accessToken: string): void {
    const connection = this.buildConnection(hubUrl, accessToken);

    connection.on('NewAdminNotification', (notification: AdminNotificationDto) => {
      this.ngZone.run(() => {
        console.log('📥 New admin notification received:', notification);
        this.notificationSubject.next(notification);
      });
    });

    this.notificationConnection = connection;
    connection
      .start()
      .then(() => {
        this.isConnected = true;
        console.log('✅ SignalR Admin Notification Connected!');
      })
      .catch((err: any) => {
        console.error('❌ Notification SignalR connection error:', err);
        this.isConnected = false;
        this.notificationConnection = null;
      });

    this.wireLifecycle(connection, 'admin');
  }

  private createMerchantConnection(hubUrl: string, accessToken: string): void {
    const connection = this.buildConnection(hubUrl, accessToken);

    connection.on('NewMerchantNotification', (notification: MerchantNotificationDto) => {
      this.ngZone.run(() => {
        console.log('📥 New merchant notification received:', notification);
        this.merchantNotificationSubject.next(notification);
      });
    });

    this.merchantNotificationConnection = connection;
    connection
      .start()
      .then(() => {
        this.isMerchantConnected = true;
        console.log('✅ SignalR Merchant Notification Connected!');
      })
      .catch((err: any) => {
        console.error('❌ Merchant Notification SignalR connection error:', err);
        this.isMerchantConnected = false;
        this.merchantNotificationConnection = null;
      });

    this.wireLifecycle(connection, 'merchant');
  }

  private buildConnection(hubUrl: string, accessToken: string): HubConnection {
    return new HubConnectionBuilder()
      .configureLogging(LogLevel.Information)
      .withUrl(hubUrl, {
        accessTokenFactory: () => {
          const token = localStorage.getItem('auth_token') || accessToken;
          return Promise.resolve(token || '');
        },
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .build();
  }

  private wireLifecycle(connection: HubConnection, kind: 'admin' | 'merchant'): void {
    connection.onreconnecting(() => {
      if (kind === 'admin') {
        this.isConnected = false;
      } else {
        this.isMerchantConnected = false;
      }
    });

    connection.onreconnected(() => {
      if (kind === 'admin') {
        this.isConnected = true;
      } else {
        this.isMerchantConnected = true;
      }
    });

    connection.onclose(() => {
      if (kind === 'admin') {
        this.isConnected = false;
        this.notificationConnection = null;
      } else {
        this.isMerchantConnected = false;
        this.merchantNotificationConnection = null;
      }
    });
  }

  public ListenForNotifications(): Observable<AdminNotificationDto | null> {
    return this.notification$;
  }

  public ListenForMerchantNotifications(): Observable<MerchantNotificationDto | null> {
    return this.merchantNotification$;
  }

  public StopNotificationConnection(): void {
    if (this.notificationConnection) {
      this.notificationConnection
        .stop()
        .then(() => {
          this.notificationConnection = null;
          this.isConnected = false;
        })
        .catch((err: any) => console.error('Error stopping Notification SignalR connection:', err));
    }

    if (this.merchantNotificationConnection) {
      this.merchantNotificationConnection
        .stop()
        .then(() => {
          this.merchantNotificationConnection = null;
          this.isMerchantConnected = false;
        })
        .catch((err: any) => console.error('Error stopping Merchant Notification SignalR connection:', err));
    }
  }
}
