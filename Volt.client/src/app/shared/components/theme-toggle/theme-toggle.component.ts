import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../../core/services/theme.service';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <button
      type="button"
      class="theme-toggle"
      (click)="themeService.toggleTheme()"
      [attr.aria-label]="(themeService.isDark() ? 'common.lightMode' : 'common.darkMode') | t"
      [title]="(themeService.isDark() ? 'common.lightMode' : 'common.darkMode') | t"
    >
      <svg *ngIf="themeService.isDark()" class="theme-toggle__icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75"
          d="M12 3v1.5M12 19.5V21M4.22 4.22l1.06 1.06M18.72 18.72l1.06 1.06M3 12h1.5M19.5 12H21M4.22 19.78l1.06-1.06M18.72 5.28l1.06-1.06M16.5 12a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0z"/>
      </svg>
      <svg *ngIf="!themeService.isDark()" class="theme-toggle__icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75"
          d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/>
      </svg>
    </button>
  `,
  styles: [`
    :host { display: inline-flex; }
    .theme-toggle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      border-radius: 0.75rem;
      border: 1px solid var(--volt-border);
      background: var(--volt-surface);
      color: var(--volt-text-secondary);
      transition: background 0.2s ease, color 0.2s ease, border-color 0.2s ease, transform 0.15s ease;
      cursor: pointer;
    }
    .theme-toggle:hover {
      background: var(--volt-surface-hover);
      color: var(--volt-accent);
      border-color: var(--volt-accent-muted);
    }
    .theme-toggle:active { transform: scale(0.96); }
    .theme-toggle__icon { width: 1.25rem; height: 1.25rem; }
  `]
})
export class ThemeToggleComponent {
  readonly themeService = inject(ThemeService);
  readonly localeService = inject(LocaleService);
}
