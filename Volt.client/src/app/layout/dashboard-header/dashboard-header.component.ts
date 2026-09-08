import { Component, EventEmitter, Input, Output, HostListener, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AdminNotificationService, AdminNotification } from '../../core/services/admin-notification.service';
import { NotificationDropdownComponent, Notification } from './notification-dropdown/notification-dropdown.component';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle.component';
import { LangSwitcherComponent } from '../../shared/components/lang-switcher/lang-switcher.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-dashboard-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NotificationDropdownComponent,
    ThemeToggleComponent,
    LangSwitcherComponent,
    TranslatePipe
  ],
  templateUrl: './dashboard-header.component.html',
  styleUrl: './dashboard-header.component.css'
})
export class DashboardHeaderComponent implements OnInit, OnDestroy {
  @Input() isMobileNavOpen = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  isUserMenuOpen = false;
  isNotificationOpen = false;
  userData: any;

  notifications: Notification[] = [];
  unreadCount = 0;
  private subscriptions = new Subscription();

  constructor(
    private authService: AuthService,
    private notificationService: AdminNotificationService
  ) {
    this.userData = this.authService.getUserData();
  }

  get avatarInitial(): string {
    const name = this.userData?.userName || 'A';
    return String(name).charAt(0).toUpperCase();
  }

  ngOnInit(): void {
    this.loadNotifications();
    this.loadUnreadCount();

    this.subscriptions.add(
      this.notificationService.notifications$.subscribe(notifications => {
        this.notifications = notifications.map(n => this.mapToNotification(n));
      })
    );

    this.subscriptions.add(
      this.notificationService.unreadCount$.subscribe(count => {
        this.unreadCount = count;
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadNotifications(): void {
    this.notificationService.loadNotifications(false, 0, 50).subscribe();
  }

  loadUnreadCount(): void {
    this.notificationService.updateUnreadCount();
  }

  mapToNotification(adminNotification: AdminNotification): Notification {
    return {
      id: adminNotification.adminNotificationId.toString(),
      type: this.getNotificationType(adminNotification.notificationType),
      title: adminNotification.title,
      message: adminNotification.message,
      timestamp: adminNotification.createdDate,
      isRead: adminNotification.isRead,
      actionUrl: adminNotification.orderId ? `/main/orders/${adminNotification.orderId}` : undefined
    };
  }

  getNotificationType(notificationType: number): 'system' | 'user' | 'order' | 'warning' | 'info' {
    switch (notificationType) {
      case 1:
      case 2:
      case 3:
      case 4:
      case 5:
      case 6:
        return 'order';
      default:
        return 'info';
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.dashboard-header__notification-menu') &&
        !target.closest('.notification-dropdown')) {
      this.isNotificationOpen = false;
    }
    if (!target.closest('.dashboard-header__user-menu') &&
        !target.closest('.dashboard-header__dropdown')) {
      this.isUserMenuOpen = false;
    }
  }

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  onToggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.isUserMenuOpen = !this.isUserMenuOpen;
    this.isNotificationOpen = false;
  }

  onToggleNotification(event: Event): void {
    event.stopPropagation();
    this.isNotificationOpen = !this.isNotificationOpen;
    this.isUserMenuOpen = false;
    if (this.isNotificationOpen) {
      this.loadNotifications();
      this.loadUnreadCount();
    }
  }

  onMarkAsRead(notificationId: string): void {
    const id = parseInt(notificationId, 10);
    this.notificationService.markAsRead(id).subscribe({
      next: () => undefined,
      error: (error) => console.error('Error marking notification as read:', error)
    });
  }

  onMarkAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => undefined,
      error: (error) => console.error('Error marking all notifications as read:', error)
    });
  }

  onNotificationClick(notification: Notification): void {
    if (!notification.isRead) {
      this.onMarkAsRead(notification.id);
    }
    this.isNotificationOpen = false;
  }

  onLogout(): void {
    this.authService.logout();
  }
}
