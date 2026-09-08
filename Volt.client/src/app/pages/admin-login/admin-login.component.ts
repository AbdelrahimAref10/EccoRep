import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LoginCommand } from '../../core/services/clientAPI';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle.component';
import { LangSwitcherComponent } from '../../shared/components/lang-switcher/lang-switcher.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { LocaleService } from '../../core/services/locale.service';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonComponent,
    ThemeToggleComponent,
    LangSwitcherComponent,
    TranslatePipe
  ],
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.css'
})
export class AdminLoginComponent implements OnInit {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  readonly currentYear = new Date().getFullYear();

  private readonly localeService = inject(LocaleService);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  ngOnInit(): void {
    void this.localeService.ensureAdminLocale();

    if (this.authService.isAuthenticated()) {
      const home = this.authService.getHomeRouteForCurrentUser();
      if (home) {
        this.router.navigate([home]);
      } else {
        this.authService.logout();
      }
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const credentials = new LoginCommand();
    credentials.userName = this.loginForm.value.userName;
    credentials.password = this.loginForm.value.password;
    credentials.role = 0; // let backend resolve role from Identity

    this.authService.login(credentials).subscribe({
      next: () => {
        this.isLoading = false;
        const home = this.authService.getHomeRouteForCurrentUser();
        if (!home) {
          this.authService.clearSession();
          this.errorMessage = this.localeService.translate('login.accessDenied');
          return;
        }
        this.router.navigate([home]);
      },
      error: (error) => {
        this.isLoading = false;
        const extractedMessage =
          error?.result?.errorMessage ||
          error?.errorMessage ||
          error?.message ||
          '';

        const errorMsgLower = String(extractedMessage).toLowerCase();
        if (
          errorMsgLower.includes('invalid') &&
          (errorMsgLower.includes('user') || errorMsgLower.includes('password') || errorMsgLower.includes('name'))
        ) {
          this.errorMessage = this.localeService.translate('login.wrongCredentials');
        } else if (errorMsgLower.includes('not active') || errorMsgLower.includes('not authorized') || errorMsgLower.includes('no valid')) {
          this.errorMessage = extractedMessage;
        } else if (error?.status === 0) {
          this.errorMessage = this.localeService.translate('login.serverUnreachable');
        } else if (error?.status === 500) {
          this.errorMessage = this.localeService.translate('login.serverError');
        } else if (extractedMessage && extractedMessage !== 'A server side error occurred.') {
          this.errorMessage = extractedMessage;
        } else if (error?.status === 400) {
          this.errorMessage = this.localeService.translate('login.wrongCredentials');
        } else {
          this.errorMessage = this.localeService.translate('login.wrongCredentials');
        }
      }
    });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });
  }

  get userName() {
    return this.loginForm.get('userName');
  }

  get password() {
    return this.loginForm.get('password');
  }

  get userNameError(): string {
    this.localeService.locale();
    if (this.userName?.hasError('required')) {
      return this.localeService.translate('login.usernameRequired');
    }
    if (this.userName?.hasError('minlength')) {
      return this.localeService.translate('login.usernameMinLength');
    }
    return '';
  }

  get passwordError(): string {
    this.localeService.locale();
    if (this.password?.hasError('required')) {
      return this.localeService.translate('login.passwordRequired');
    }
    if (this.password?.hasError('minlength')) {
      return this.localeService.translate('login.passwordMinLength');
    }
    return '';
  }
}
