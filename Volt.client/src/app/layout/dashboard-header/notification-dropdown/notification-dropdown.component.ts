import { Component, Input, Output, EventEmitter, OnInit, OnChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { LocaleService } from '../../../core/services/locale.service';

export interface Notification {
  id: string;
  type: 'system' | 'user' | 'order' | 'warning' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
  actionUrl?: string;
}

@Component({
  selector: 'app-notification-dropdown',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './notification-dropdown.component.html',
  styleUrl: './notification-dropdown.component.css'
})
export class NotificationDropdownComponent implements OnInit, OnChanges {
  @Input() notifications: Notification[] = [];
  @Input() isOpen: boolean = false;
  @Output() markAsRead = new EventEmitter<string>();
  @Output() markAllAsRead = new EventEmitter<void>();
  @Output() notificationClick = new EventEmitter<Notification>();

  unreadCount: number = 0;
  private readonly localeService = inject(LocaleService);

  ngOnInit(): void {
    this.updateUnreadCount();
  }

  ngOnChanges(): void {
    this.updateUnreadCount();
  }

  updateUnreadCount(): void {
    this.unreadCount = this.notifications.filter(n => !n.isRead).length;
  }

  onMarkAsRead(notificationId: string, event: Event): void {
    event.stopPropagation();
    this.markAsRead.emit(notificationId);
  }

  onMarkAllAsRead(): void {
    this.markAllAsRead.emit();
  }

  onNotificationClick(notification: Notification): void {
    if (!notification.isRead) {
      this.markAsRead.emit(notification.id);
    }
    this.notificationClick.emit(notification);
    // Navigation will be handled by parent component if actionUrl is provided
  }

  getTimeAgo(timestamp: Date): string {
    this.localeService.locale();
    const now = new Date();
    const diff = now.getTime() - new Date(timestamp).getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) {
      return this.localeService.translate(days === 1 ? 'common.dayAgo' : 'common.daysAgo', { count: days });
    }
    if (hours > 0) {
      return this.localeService.translate(hours === 1 ? 'common.hourAgo' : 'common.hoursAgo', { count: hours });
    }
    if (minutes > 0) {
      return this.localeService.translate(minutes === 1 ? 'common.minuteAgo' : 'common.minutesAgo', { count: minutes });
    }
    return this.localeService.translate('common.justNow');
  }

  getNotificationIcon(type: string): string {
    switch (type) {
      case 'system':
        return 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z';
      case 'user':
        return 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z';
      case 'order':
        return 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2';
      case 'warning':
        return 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z';
      case 'info':
        return 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z';
      default:
        return 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9';
    }
  }

  getNotificationColor(type: string): string {
    switch (type) {
      case 'system':
        return 'text-primary-600 bg-primary-50';
      case 'user':
        return 'text-blue-600 bg-blue-50';
      case 'order':
        return 'text-success-600 bg-success-50';
      case 'warning':
        return 'text-warning-600 bg-warning-50';
      case 'info':
        return 'text-info-600 bg-info-50';
      default:
        return 'text-neutral-600 bg-neutral-50';
    }
  }
}

