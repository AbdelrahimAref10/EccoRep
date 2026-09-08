import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Requires authenticated Super Admin for admin dashboard routes. */
export const superAdminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (auth.isSuperAdmin()) {
    return true;
  }

  if (auth.isMerchant()) {
    return router.createUrlTree(['/merchant']);
  }

  auth.logout();
  return router.createUrlTree(['/login']);
};

/** Requires authenticated Merchant for merchant panel routes. */
export const merchantGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (auth.isMerchant()) {
    return true;
  }

  if (auth.isSuperAdmin()) {
    return router.createUrlTree(['/main']);
  }

  auth.logout();
  return router.createUrlTree(['/login']);
};
