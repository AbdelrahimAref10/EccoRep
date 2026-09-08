import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

interface MenuItem {
  labelKey: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-dashboard-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './dashboard-sidebar.component.html',
  styleUrl: './dashboard-sidebar.component.css'
})
export class DashboardSidebarComponent {
  @Input() isOpen: boolean = true;
  @Input() isMobileOpen: boolean = false;
  @Output() closeMobile: EventEmitter<void> = new EventEmitter<void>();

  menuItems: MenuItem[] = [
    { labelKey: 'nav.dashboard', route: '/main/dashboard', icon: 'dashboard' },
    { labelKey: 'nav.categories', route: '/main/categories', icon: 'categories' },
    { labelKey: 'nav.subcategories', route: '/main/subcategories', icon: 'subcategories' },
    { labelKey: 'nav.vehicles', route: '/main/vehicles', icon: 'vehicles' },
    { labelKey: 'nav.customers', route: '/main/customers', icon: 'users' },
    { labelKey: 'nav.systemUsers', route: '/main/users', icon: 'system-users' },
    { labelKey: 'nav.roles', route: '/main/roles', icon: 'roles' },
    { labelKey: 'nav.cities', route: '/main/cities', icon: 'cities' },
    { labelKey: 'nav.orders', route: '/main/orders', icon: 'orders' },
    { labelKey: 'nav.reports', route: '/main/reports', icon: 'reports' },
    { labelKey: 'nav.support', route: '/main/support', icon: 'support' },
  ];

  onCloseMobile(): void {
    this.closeMobile.emit();
  }

  onNavClick(): void {
    if (this.isMobileOpen) {
      this.closeMobile.emit();
    }
  }
}
