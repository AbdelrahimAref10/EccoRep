import { Component, HostListener, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { ThemeService } from '../../../../core/services/theme.service';
import { AppLocale, LocaleService } from '../../../../core/services/locale.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-landing-header',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './landing-header.component.html',
  styleUrl: './landing-header.component.css'
})
export class LandingHeaderComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private routeSub?: Subscription;
  readonly theme = inject(ThemeService);
  readonly localeService = inject(LocaleService);
  readonly menuOpen = signal(false);
  readonly langOpen = signal(false);
  readonly scrolled = signal(false);

  ngOnInit(): void {
    this.onWindowScroll();
    this.routeSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.onWindowScroll());
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  readonly navItems = [
    { key: 'landing.nav.features', href: '#features' },
    { key: 'landing.nav.why', href: '#why' },
    { key: 'landing.nav.vehicles', href: '#vehicles' },
    { key: 'landing.nav.contact', href: '#contact' }
  ];

  @HostListener('window:scroll')
  onWindowScroll(): void {
    const onLandingHero = this.router.url === '/' || this.router.url.startsWith('/#');
    this.scrolled.set(window.scrollY > 24 || !onLandingHero);
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.lp-header__lang')) {
      this.langOpen.set(false);
    }
  }

  toggleMenu(): void {
    this.menuOpen.update(v => !v);
  }

  toggleTheme(): void {
    this.theme.toggleTheme();
  }

  toggleLang(): void {
    this.langOpen.update(v => !v);
  }

  async setLocale(code: AppLocale): Promise<void> {
    await this.localeService.setLocale(code);
    this.langOpen.set(false);
    this.menuOpen.set(false);
  }

  scrollTo(href: string): void {
    this.menuOpen.set(false);
    const id = href.replace('#', '');
    const onHome = this.router.url === '/' || this.router.url.startsWith('/#');
    const el = document.getElementById(id);

    if (onHome && el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      return;
    }

    void this.router.navigate(['/'], { fragment: id === 'top' ? undefined : id });
  }
}
