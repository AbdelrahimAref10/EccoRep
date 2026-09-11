import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthService } from './auth.service';
import { SignalRService, MerchantNotificationDto } from './signalr.service';

export interface MerchantNotification {
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
export class MerchantNotificationService {
  private notificationsSubject = new BehaviorSubject<MerchantNotification[]>([]);
  public notifications$: Observable<MerchantNotification[]> = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$: Observable<number> = this.unreadCountSubject.asObservable();

  /** Fires when a realtime merchant notification arrives (for page refresh). */
  private incomingSubject = new BehaviorSubject<MerchantNotification | null>(null);
  public incoming$: Observable<MerchantNotification | null> = this.incomingSubject.asObservable();

  private readonly apiBaseUrl = '/api/Merchant/notifications';

  constructor(
    private http: HttpClient,
    private signalRService: SignalRService,
    private authService: AuthService
  ) {
    this.signalRService.ListenForMerchantNotifications().subscribe(notification => {
      if (!notification) {
        return;
      }

      const myMerchantId = this.authService.getUserData()?.merchantId;
      if (myMerchantId && notification.merchantId !== myMerchantId) {
        return;
      }

      const mapped = this.mapToNotification(notification);
      this.playNotificationSound();
      this.addNotification(mapped);
      this.incomingSubject.next(mapped);
    });
  }

  loadNotifications(isRead?: boolean, skip?: number, take?: number): Observable<MerchantNotification[]> {
    let params = new HttpParams();
    if (isRead !== undefined) {
      params = params.set('isRead', isRead.toString());
    }
    if (skip !== undefined) {
      params = params.set('skip', skip.toString());
    }
    if (take !== undefined) {
      params = params.set('take', take.toString());
    }

    return this.http.get<MerchantNotificationDto[]>(this.apiBaseUrl, { params }).pipe(
      tap(notifications => {
        const mapped = notifications.map(n => this.mapToNotification(n));
        mapped.sort((a, b) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime());
        this.notificationsSubject.next(mapped);
        this.updateUnreadCount();
      })
    );
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiBaseUrl}/UnreadCount`).pipe(
      tap(count => this.unreadCountSubject.next(count))
    );
  }

  markAsRead(id: number): Observable<any> {
    return this.http.post(`${this.apiBaseUrl}/${id}/MarkAsRead`, {}).pipe(
      tap(() => {
        const notifications = this.notificationsSubject.value.map(n =>
          n.merchantNotificationId === id ? { ...n, isRead: true, readAt: new Date() } : n
        );
        this.notificationsSubject.next(notifications);
        this.updateUnreadCount();
      })
    );
  }

  markAllAsRead(): Observable<any> {
    return this.http.post(`${this.apiBaseUrl}/MarkAllAsRead`, {}).pipe(
      tap(() => {
        const now = new Date();
        const notifications = this.notificationsSubject.value.map(n =>
          n.isRead ? n : { ...n, isRead: true, readAt: now }
        );
        this.notificationsSubject.next(notifications);
        this.unreadCountSubject.next(0);
      })
    );
  }

  updateUnreadCount(): void {
    this.getUnreadCount().subscribe();
  }

  private addNotification(notification: MerchantNotification): void {
    const current = this.notificationsSubject.value;
    const exists = current.some(n => n.merchantNotificationId === notification.merchantNotificationId);
    if (!exists) {
      this.notificationsSubject.next([notification, ...current]);
      if (!notification.isRead) {
        this.updateUnreadCount();
      }
    }
  }

  private mapToNotification(dto: MerchantNotificationDto): MerchantNotification {
    return {
      merchantNotificationId: dto.merchantNotificationId,
      merchantId: dto.merchantId,
      title: dto.title,
      message: dto.message,
      orderId: dto.orderId,
      orderCode: dto.orderCode,
      notificationType: dto.notificationType,
      isRead: dto.isRead,
      readAt: dto.readAt ? new Date(dto.readAt) : undefined,
      createdDate: new Date(dto.createdDate)
    };
  }

  private playNotificationSound(): void {
    try {
      const audio = new Audio('assets/sounds/notification.wav');
      audio.volume = 0.9;
      audio.play().catch(error => {
        console.warn('Could not play notification sound:', error);
      });
    } catch (error) {
      console.warn('Error creating audio element:', error);
    }
  }
}
