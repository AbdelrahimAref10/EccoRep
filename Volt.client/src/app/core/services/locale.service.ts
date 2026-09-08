import { ApplicationRef, Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export type AppLocale = 'en' | 'ar' | 'de' | 'fr' | 'ru';

const STORAGE_KEY = 'volt-locale';
const SUPPORTED: AppLocale[] = ['en', 'ar', 'de', 'fr', 'ru'];
/** Admin panel + admin login: Arabic & English only */
const ADMIN_LOCALES: AppLocale[] = ['en', 'ar'];

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly translations = signal<Record<string, unknown>>({});
  private readonly englishFallback = signal<Record<string, unknown>>({});
  private readonly currentLocale = signal<AppLocale>(this.readStoredLocale());

  readonly locale = this.currentLocale.asReadonly();
  readonly isRtl = computed(() => this.currentLocale() === 'ar');
  readonly ready = signal(false);

  /** Full set — used by landing page language menu */
  readonly locales: { code: AppLocale; label: string; native: string }[] = [
    { code: 'en', label: 'English', native: 'English' },
    { code: 'ar', label: 'Arabic', native: 'العربية' },
    { code: 'de', label: 'German', native: 'Deutsch' },
    { code: 'fr', label: 'French', native: 'Français' },
    { code: 'ru', label: 'Russian', native: 'Русский' }
  ];

  readonly adminLocales = this.locales.filter(l => ADMIN_LOCALES.includes(l.code));

  constructor(
    private http: HttpClient,
    private appRef: ApplicationRef
  ) {}

  async init(): Promise<void> {
    const en = await firstValueFrom(
      this.http.get<Record<string, unknown>>('assets/i18n/en.json')
    );
    this.englishFallback.set(en);

    await this.loadLocale(this.currentLocale());
    this.applyDocumentLocale(this.currentLocale());
    this.ready.set(true);
  }

  async setLocale(locale: AppLocale): Promise<void> {
    if (!SUPPORTED.includes(locale)) {
      return;
    }
    if (locale === this.currentLocale()) {
      return;
    }
    await this.loadLocale(locale);
    this.currentLocale.set(locale);
    localStorage.setItem(STORAGE_KEY, locale);
    this.applyDocumentLocale(locale);
    this.appRef.tick();
  }

  /** Landing page: cycle all supported locales */
  toggleLocale(): Promise<void> {
    const order: AppLocale[] = [...SUPPORTED];
    const idx = order.indexOf(this.currentLocale());
    const next = order[(idx + 1) % order.length];
    return this.setLocale(next);
  }

  /** Admin UI: English ↔ Arabic only */
  toggleAdminLocale(): Promise<void> {
    const current = this.currentLocale();
    const next: AppLocale = current === 'ar' ? 'en' : 'ar';
    return this.setLocale(next);
  }

  /** If user left landing on DE/FR/RU, reset to English for admin */
  ensureAdminLocale(): Promise<void> {
    if (ADMIN_LOCALES.includes(this.currentLocale())) {
      return Promise.resolve();
    }
    return this.setLocale('en');
  }

  translate(key: string, params?: Record<string, string | number>): string {
    const value = this.resolveKey(key, this.translations())
      ?? this.resolveKey(key, this.englishFallback());
    if (value == null) {
      return key;
    }
    if (!params) {
      return value;
    }
    return Object.keys(params).reduce(
      (text, param) => text.replace(new RegExp(`{{\\s*${param}\\s*}}`, 'g'), String(params[param])),
      value
    );
  }

  private async loadLocale(locale: AppLocale): Promise<void> {
    if (locale === 'en') {
      this.translations.set(this.englishFallback());
      return;
    }
    try {
      const data = await firstValueFrom(
        this.http.get<Record<string, unknown>>(`assets/i18n/${locale}.json`)
      );
      this.translations.set(data);
    } catch {
      this.translations.set(this.englishFallback());
    }
  }

  private resolveKey(key: string, tree: Record<string, unknown>): string | null {
    const parts = key.split('.');
    let current: unknown = tree;
    for (const part of parts) {
      if (current == null || typeof current !== 'object') {
        return null;
      }
      current = (current as Record<string, unknown>)[part];
    }
    return typeof current === 'string' ? current : null;
  }

  private applyDocumentLocale(locale: AppLocale): void {
    const html = document.documentElement;
    html.lang = locale;
    html.dir = locale === 'ar' ? 'rtl' : 'ltr';
    for (const code of SUPPORTED) {
      html.classList.toggle(`locale-${code}`, locale === code);
    }
  }

  private readStoredLocale(): AppLocale {
    const stored = localStorage.getItem(STORAGE_KEY);
    return SUPPORTED.includes(stored as AppLocale) ? (stored as AppLocale) : 'en';
  }
}
