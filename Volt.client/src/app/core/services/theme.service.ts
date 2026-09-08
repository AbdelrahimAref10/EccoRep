import { Injectable, signal, computed } from '@angular/core';

export type AppTheme = 'light' | 'dark';

const STORAGE_KEY = 'volt-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly currentTheme = signal<AppTheme>(this.resolveInitialTheme());

  readonly theme = this.currentTheme.asReadonly();
  readonly isDark = computed(() => this.currentTheme() === 'dark');

  constructor() {
    this.applyTheme(this.currentTheme());
  }

  setTheme(theme: AppTheme): void {
    this.currentTheme.set(theme);
    localStorage.setItem(STORAGE_KEY, theme);
    this.applyTheme(theme);
  }

  toggleTheme(): void {
    this.setTheme(this.currentTheme() === 'dark' ? 'light' : 'dark');
  }

  private applyTheme(theme: AppTheme): void {
    const root = document.documentElement;
    root.classList.toggle('dark', theme === 'dark');
    root.dataset['theme'] = theme;
  }

  private resolveInitialTheme(): AppTheme {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
