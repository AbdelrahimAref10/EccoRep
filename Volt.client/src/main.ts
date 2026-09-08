import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideToastr } from 'ngx-toastr';
import { AppComponent } from './app/app.component';
import { appRouterProviders } from './app/app.routes';
import { API_BASE_URL } from './app/core/services/clientAPI';
import { AuthInterceptor } from './app/core/interceptors/auth.interceptor';
import { ErrorInterceptor } from './app/core/interceptors/error.interceptor';
import { LocaleService } from './app/core/services/locale.service';
import { ThemeService } from './app/core/services/theme.service';
import { APP_INITIALIZER } from '@angular/core';

function initializeApp(localeService: LocaleService, themeService: ThemeService) {
  return () => {
    // Theme applies immediately in ThemeService constructor
    void themeService;
    return localeService.init();
  };
}

bootstrapApplication(AppComponent, {
  providers: [
    appRouterProviders,
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    provideToastr(),
    {
      provide: API_BASE_URL,
      useValue: ''
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [LocaleService, ThemeService],
      multi: true
    }
  ]
}).catch(err => console.error(err));
