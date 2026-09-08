import { Component, HostListener, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-landing-hero',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './landing-hero.component.html',
  styleUrl: './landing-hero.component.css'
})
export class LandingHeroComponent {
  readonly bgTransform = signal('scale(1.08)');

  @HostListener('window:scroll')
  onScroll(): void {
    if (typeof window === 'undefined') return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      this.bgTransform.set('scale(1.04)');
      return;
    }
    const y = Math.min(window.scrollY, 480);
    const shift = y * 0.28;
    const scale = 1.08 + y * 0.00015;
    this.bgTransform.set(`translate3d(0, ${shift}px, 0) scale(${scale})`);
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
