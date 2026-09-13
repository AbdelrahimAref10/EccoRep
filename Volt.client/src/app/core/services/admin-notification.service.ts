import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { SignalRService, AdminNotificationDto } from './signalr.service';

export interface AdminNotification {
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
export class AdminNotificationService {
  private notificationsSubject = new BehaviorSubject<AdminNotification[]>([]);
  public notifications$: Observable<AdminNotification[]> = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$: Observable<number> = this.unreadCountSubject.asObservable();

  /** Fires when a realtime admin notification arrives (for page refresh). */
  private incomingSubject = new BehaviorSubject<AdminNotification | null>(null);
  public incoming$: Observable<AdminNotification | null> = this.incomingSubject.asObservable();

  private readonly apiBaseUrl = '/api/AdminNotification';

  constructor(
    private http: HttpClient,
    private signalRService: SignalRService
  ) {
    // Subscribe to SignalR notifications
    this.signalRService.ListenForNotifications().subscribe(notification => {
      if (notification) {
        console.log('📥 Adding notification from SignalR:', notification);
        this.playNotificationSound();
        const mapped = this.mapToNotification(notification);
        this.addNotification(mapped);
        this.incomingSubject.next(mapped);
      }
    });
  }

  // Load notifications from API
  loadNotifications(isRead?: boolean, skip?: number, take?: number): Observable<AdminNotification[]> {
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

    return this.http.get<AdminNotificationDto[]>(this.apiBaseUrl, { params })
      .pipe(
        tap(notifications => {
          const mapped = notifications.map(n => this.mapToNotification(n));
          // Sort by created date (newest first)
          mapped.sort((a, b) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime());
          this.notificationsSubject.next(mapped);
          this.updateUnreadCount();
        })
      );
  }

  // Get unread count
  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiBaseUrl}/UnreadCount`)
      .pipe(
        tap(count => {
          this.unreadCountSubject.next(count);
        })
      );
  }

  // Mark as read
  markAsRead(id: number): Observable<any> {
    return this.http.post(`${this.apiBaseUrl}/${id}/MarkAsRead`, {})
      .pipe(
        tap(() => {
          const notifications = this.notificationsSubject.value.map(n =>
            n.adminNotificationId === id
              ? { ...n, isRead: true, readAt: new Date() }
              : n
          );
          this.notificationsSubject.next(notifications);
          this.updateUnreadCount();
        })
      );
  }

  // Mark all as read
  markAllAsRead(): Observable<any> {
    return this.http.post(`${this.apiBaseUrl}/MarkAllAsRead`, {})
      .pipe(
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

  // Add notification to list (from SignalR)
  private addNotification(notification: AdminNotification): void {
    const current = this.notificationsSubject.value;
    // Check if notification already exists (avoid duplicates)
    const exists = current.some(n => n.adminNotificationId === notification.adminNotificationId);
    if (!exists) {
      const updated = [notification, ...current];
      this.notificationsSubject.next(updated);

      // Update unread count
      if (!notification.isRead) {
        this.updateUnreadCount();
      }
    }
  }

  // Update unread count
  updateUnreadCount(): void {
    this.getUnreadCount().subscribe();
  }

  // Map DTO to Notification interface
  private mapToNotification(dto: AdminNotificationDto): AdminNotification {
    return {
      adminNotificationId: dto.adminNotificationId,
      title: dto.title,
      message: dto.message,
      orderId: dto.orderId,
      orderCode: dto.orderCode,
      notificationType: dto.notificationType,
      isRead: dto.isRead,
      readAt: dto.readAt ? new Date(dto.readAt) : undefined,
      readByUserId: dto.readByUserId,
      createdDate: new Date(dto.createdDate)
    };
  }

  // Get notification type name
  getNotificationTypeName(type: number): string {
    const types: { [key: number]: string } = {
      1: 'order',
      2: 'order',
      3: 'order',
      4: 'order',
      5: 'order',
      6: 'order',
      7: 'order',
      8: 'order'
    };
    return types[type] || 'info';
  }

  // Play notification sound
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

