import { Routes, provideRouter } from '@angular/router';
import { DashboardLayoutComponent } from './layout/dashboard-layout/dashboard-layout.component';
import { LandingLayoutComponent } from './layout/landing-layout/landing-layout.component';
import { MerchantLayoutComponent } from './layout/merchant-layout/merchant-layout.component';
import { AdminLoginComponent } from './pages/admin-login/admin-login.component';
import { adminEntryGuard } from './core/guards/admin-entry.guard';
import { merchantGuard, superAdminGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    component: LandingLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/landing/landing-page.component').then(m => m.LandingPageComponent)
      },
      {
        path: 'vehicles/:slug',
        loadComponent: () =>
          import('./pages/landing/vehicle-detail/vehicle-detail.component').then(m => m.VehicleDetailComponent)
      }
    ]
  },
  { path: 'admin', canActivate: [adminEntryGuard], children: [] },
  { path: 'admin/login', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: AdminLoginComponent },
  {
    path: 'main',
    component: DashboardLayoutComponent,
    canActivate: [superAdminGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./pages/categories/categories.component').then(m => m.CategoriesComponent)
      },
      {
        path: 'categories/new',
        loadComponent: () => import('./pages/categories/category-form/category-form.component').then(m => m.CategoryFormComponent)
      },
      {
        path: 'categories/:id/edit',
        loadComponent: () => import('./pages/categories/category-form/category-form.component').then(m => m.CategoryFormComponent)
      },
      {
        path: 'subcategories',
        loadComponent: () => import('./pages/subcategories/subcategories.component').then(m => m.SubCategoriesComponent)
      },
      {
        path: 'subcategories/new',
        loadComponent: () => import('./pages/subcategories/subcategory-form/subcategory-form.component').then(m => m.SubCategoryFormComponent)
      },
      {
        path: 'subcategories/:id/edit',
        loadComponent: () => import('./pages/subcategories/subcategory-form/subcategory-form.component').then(m => m.SubCategoryFormComponent)
      },
      {
        path: 'vehicles',
        loadComponent: () => import('./pages/vehicles/vehicles.component').then(m => m.VehiclesComponent)
      },
      {
        path: 'vehicles/new',
        loadComponent: () => import('./pages/vehicles/vehicle-form/vehicle-form.component').then(m => m.VehicleFormComponent)
      },
      {
        path: 'vehicles/:id/edit',
        loadComponent: () => import('./pages/vehicles/vehicle-form/vehicle-form.component').then(m => m.VehicleFormComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./pages/customers/customers.component').then(m => m.CustomersComponent)
      },
      {
        path: 'customers/new',
        loadComponent: () => import('./pages/customers/customer-form/customer-form.component').then(m => m.CustomerFormComponent)
      },
      {
        path: 'customers/:id',
        loadComponent: () => import('./pages/customers/customer-detail/customer-detail.component').then(m => m.CustomerDetailComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./pages/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'users/:id',
        loadComponent: () => import('./pages/users/user-detail/user-detail.component').then(m => m.UserDetailComponent)
      },
      {
        path: 'roles',
        loadComponent: () => import('./pages/roles/roles.component').then(m => m.RolesComponent)
      },
      {
        path: 'cities',
        loadComponent: () => import('./pages/cities/cities.component').then(m => m.CitiesComponent)
      },
      {
        path: 'cities/new',
        loadComponent: () => import('./pages/cities/city-form/city-form.component').then(m => m.CityFormComponent)
      },
      {
        path: 'cities/:id/edit',
        loadComponent: () => import('./pages/cities/city-form/city-form.component').then(m => m.CityFormComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./pages/orders/orders.component').then(m => m.OrdersComponent)
      },
      {
        path: 'orders/new',
        loadComponent: () => import('./pages/orders/order-form/order-form.component').then(m => m.OrderFormComponent)
      },
      {
        path: 'orders/:id/edit',
        loadComponent: () => import('./pages/orders/order-form/order-form.component').then(m => m.OrderFormComponent)
      },
      {
        path: 'orders/:id',
        loadComponent: () => import('./pages/orders/order-detail/order-detail.component').then(m => m.OrderDetailComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./pages/reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'reports/orders-details',
        loadComponent: () => import('./pages/reports/orders-details-report/orders-details-report.component').then(m => m.OrdersDetailsReportComponent)
      },
      {
        path: 'reports/cancelled-orders',
        loadComponent: () => import('./pages/reports/cancelled-orders-report/cancelled-orders-report.component').then(m => m.CancelledOrdersReportComponent)
      },
      {
        path: 'reports/cancellation-debts',
        loadComponent: () => import('./pages/reports/cancellation-debts-report/cancellation-debts-report.component').then(m => m.CancellationDebtsReportComponent)
      },
      {
        path: 'reports/payments',
        loadComponent: () => import('./pages/reports/payments-report/payments-report.component').then(m => m.PaymentsReportComponent)
      },
      {
        path: 'reports/paypal-refunds',
        loadComponent: () => import('./pages/reports/paypal-refunds-report/paypal-refunds-report.component').then(m => m.PayPalRefundsReportComponent)
      },
      {
        path: 'support',
        loadComponent: () => import('./pages/support/support.component').then(m => m.SupportComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent)
      }
    ]
  },
  {
    path: 'merchant',
    component: MerchantLayoutComponent,
    canActivate: [merchantGuard],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      {
        path: 'home',
        loadComponent: () =>
          import('./pages/merchant/merchant-home.component').then(m => m.MerchantHomeComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];

export const appRouterProviders = [provideRouter(routes)];
