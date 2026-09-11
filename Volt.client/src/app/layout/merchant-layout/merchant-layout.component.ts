import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule, RouterOutlet } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { DashboardHeaderComponent } from '../dashboard-header/dashboard-header.component';
import {
  DashboardMenuItem,
  DashboardSidebarComponent
} from '../dashboard-sidebar/dashboard-sidebar.component';
import { LocaleService } from '../../core/services/locale.service';
import { AuthService } from '../../core/services/auth.service';
import { SignalRService } from '../../core/services/signalr.service';

const COMPACT_QUERY = '(max-width: 1024px)';

@Component({
  selector: 'app-merchant-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterOutlet,
    DashboardHeaderComponent,
    DashboardSidebarComponent
  ],
  templateUrl: './merchant-layout.component.html',
  styleUrls: [
    './merchant-layout.component.css',
    '../dashboard-layout/dashboard-layout.component.css'
  ]
})
export class MerchantLayoutComponent implements OnInit, OnDestroy {
  isSidebarOpen = true;
  isMobileNavOpen = false;
  isCompactViewport = false;

  readonly menuItems: DashboardMenuItem[] = [
    { labelKey: 'merchant.nav.home', route: '/merchant/home', icon: 'dashboard' },
    { labelKey: 'merchant.nav.orders', route: '/merchant/orders', icon: 'orders' },
    { labelKey: 'merchant.nav.vehicles', route: '/merchant/vehicles', icon: 'vehicles' },
    { labelKey: 'merchant.nav.payments', route: '/merchant/payments', icon: 'reports' }
  ];

  private mediaQuery?: MediaQueryList;
  private mediaListener?: (event: MediaQueryListEvent) => void;
  private routeSub?: Subscription;

  constructor(
    private localeService: LocaleService,
    private router: Router,
    private authService: AuthService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    void this.localeService.ensureAdminLocale();

    if (this.authService.isAuthenticated()) {
      const token = this.authService.getToken();
      if (token) {
        this.signalRService.StartMerchantNotificationConnection(token);
      }
    }

    this.mediaQuery = window.matchMedia(COMPACT_QUERY);
    this.isCompactViewport = this.mediaQuery.matches;
    this.mediaListener = (event: MediaQueryListEvent) => {
      this.isCompactViewport = event.matches;
      if (!event.matches) {
        this.closeMobileNav();
      }
    };
    this.mediaQuery.addEventListener('change', this.mediaListener);

    this.routeSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.closeMobileNav());
  }

  ngOnDestroy(): void {
    if (this.mediaQuery && this.mediaListener) {
      this.mediaQuery.removeEventListener('change', this.mediaListener);
    }
    this.routeSub?.unsubscribe();
    document.body.classList.remove('dashboard-mobile-nav-open');
  }

  get sidebarExpanded(): boolean {
    return this.isCompactViewport ? true : this.isSidebarOpen;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isMobileNavOpen) {
      this.closeMobileNav();
    }
  }

  toggleSidebar(): void {
    if (this.isCompactViewport) {
      this.isMobileNavOpen = !this.isMobileNavOpen;
      this.syncBodyScrollLock();
      return;
    }
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeMobileNav(): void {
    if (!this.isMobileNavOpen) {
      return;
    }
    this.isMobileNavOpen = false;
    this.syncBodyScrollLock();
  }

  private syncBodyScrollLock(): void {
    document.body.classList.toggle('dashboard-mobile-nav-open', this.isMobileNavOpen);
  }
}
