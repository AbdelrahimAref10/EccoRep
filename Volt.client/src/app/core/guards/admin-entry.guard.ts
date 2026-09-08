import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** `/admin` → role home if logged in, otherwise login */
export const adminEntryGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const home = auth.getHomeRouteForCurrentUser();
  if (home) {
    return router.createUrlTree([home]);
  }

  auth.logout();
  return router.createUrlTree(['/login']);
};
