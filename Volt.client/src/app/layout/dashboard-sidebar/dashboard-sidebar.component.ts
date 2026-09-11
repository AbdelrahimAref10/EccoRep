import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

export interface DashboardMenuItem {
  labelKey: string;
  route: string;
  icon: string;
}

const ADMIN_MENU_ITEMS: DashboardMenuItem[] = [
  { labelKey: 'nav.dashboard', route: '/main/dashboard', icon: 'dashboard' },
  { labelKey: 'nav.categories', route: '/main/categories', icon: 'categories' },
  { labelKey: 'nav.subcategories', route: '/main/subcategories', icon: 'subcategories' },
  { labelKey: 'nav.vehicles', route: '/main/vehicles', icon: 'vehicles' },
  { labelKey: 'nav.customers', route: '/main/customers', icon: 'users' },
  { labelKey: 'nav.merchants', route: '/main/merchants', icon: 'users' },
  { labelKey: 'nav.deliveries', route: '/main/deliveries', icon: 'users' },
  { labelKey: 'nav.systemUsers', route: '/main/users', icon: 'system-users' },
  { labelKey: 'nav.roles', route: '/main/roles', icon: 'roles' },
  { labelKey: 'nav.cities', route: '/main/cities', icon: 'cities' },
  { labelKey: 'nav.orders', route: '/main/orders', icon: 'orders' },
  { labelKey: 'nav.settlements', route: '/main/settlements', icon: 'orders' },
  { labelKey: 'nav.journals', route: '/main/journals', icon: 'reports' },
  { labelKey: 'nav.reports', route: '/main/reports', icon: 'reports' },
  { labelKey: 'nav.support', route: '/main/support', icon: 'support' }
];

@Component({
  selector: 'app-dashboard-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './dashboard-sidebar.component.html',
  styleUrl: './dashboard-sidebar.component.css'
})
export class DashboardSidebarComponent {
  @Input() isOpen = true;
  @Input() isMobileOpen = false;
  @Input() menuItems: DashboardMenuItem[] = ADMIN_MENU_ITEMS;
  /** Route that should use exact routerLinkActive matching (home/dashboard). */
  @Input() exactActiveRoute = '/main/dashboard';
  @Input() footerKey = 'app.adminFooter';
  @Output() closeMobile: EventEmitter<void> = new EventEmitter<void>();

  onCloseMobile(): void {
    this.closeMobile.emit();
  }

  onNavClick(): void {
    if (this.isMobileOpen) {
      this.closeMobile.emit();
    }
  }
}
