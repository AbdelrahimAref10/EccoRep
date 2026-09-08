import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-lang-switcher',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <button
      type="button"
      class="lang-switcher"
      (click)="onToggle()"
      [attr.aria-label]="'common.language' | t"
      [title]="'common.language' | t"
    >
      <span class="lang-switcher__code">{{ currentLabel }}</span>
    </button>
  `,
  styles: [`
    :host { display: inline-flex; }
    .lang-switcher {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 2.5rem;
      height: 2.5rem;
      padding: 0 0.65rem;
      border-radius: 0.75rem;
      border: 1px solid var(--volt-border);
      background: var(--volt-surface);
      color: var(--volt-text-primary);
      font-family: inherit;
      font-size: 0.75rem;
      font-weight: 700;
      letter-spacing: 0.04em;
      transition: background 0.2s ease, color 0.2s ease, border-color 0.2s ease, transform 0.15s ease;
      cursor: pointer;
    }
    .lang-switcher:hover {
      background: var(--volt-surface-hover);
      color: var(--volt-accent);
      border-color: var(--volt-accent-muted);
    }
    .lang-switcher:active { transform: scale(0.96); }
    .lang-switcher__code { line-height: 1; }
  `]
})
export class LangSwitcherComponent {
  private readonly localeService = inject(LocaleService);

  /** Admin-only switcher: EN ↔ AR */
  get currentLabel(): string {
    return this.localeService.locale() === 'ar' ? 'ع' : 'EN';
  }

  async onToggle(): Promise<void> {
    await this.localeService.toggleAdminLocale();
  }
}
