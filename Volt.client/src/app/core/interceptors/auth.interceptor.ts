import { Injectable, Injector } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private injector: Injector) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Don't add token to login/register/activate endpoints
    const isAuthEndpoint = req.url.includes('/Auth/') ||
                          req.url.includes('/Login') ||
                          req.url.includes('/Register') ||
                          req.url.includes('/Activate') ||
                          req.url.includes('/ForgetPassword') ||
                          req.url.includes('/ResetPassword') ||
                          req.url.includes('/RefreshToken');

    // Don't add token to static assets (like appSettings.json)
    // Check if URL is a static asset by looking for assets/ path or relative asset paths
    const isStaticAsset = req.url.startsWith('assets/') ||
                         req.url.includes('/assets/') ||
                         (!req.url.startsWith('http') && !req.url.startsWith('/api') &&
                          (req.url.endsWith('.json') || req.url.endsWith('.html') ||
                           req.url.endsWith('.css') || req.url.endsWith('.js')));

    if (isAuthEndpoint || isStaticAsset) {
      return next.handle(req);
    }

    // Use Injector to lazily get AuthService to avoid circular dependency
    const authService = this.injector.get(AuthService);
    const token = authService.getToken();

    if (token) {
      // Clone the request and add the authorization header
      const cloned = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next.handle(cloned);
    }

    return next.handle(req);
  }
}

