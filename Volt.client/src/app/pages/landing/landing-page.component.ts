import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LandingHeaderComponent } from './sections/landing-header/landing-header.component';
import { LandingHeroComponent } from './sections/landing-hero/landing-hero.component';
import { LandingFeaturesComponent } from './sections/landing-features/landing-features.component';
import { LandingWhyComponent } from './sections/landing-why/landing-why.component';
import { LandingScootersComponent } from './sections/landing-scooters/landing-scooters.component';
import { LandingCtaComponent } from './sections/landing-cta/landing-cta.component';
import { LandingFooterComponent } from './sections/landing-footer/landing-footer.component';
import { ThemeService } from '../../core/services/theme.service';
import { LocaleService } from '../../core/services/locale.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [
    CommonModule,
    LandingHeaderComponent,
    LandingHeroComponent,
    LandingFeaturesComponent,
    LandingWhyComponent,
    LandingScootersComponent,
    LandingCtaComponent,
    LandingFooterComponent
  ],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.css'
})
export class LandingPageComponent {
  readonly theme = inject(ThemeService);
  readonly locale = inject(LocaleService);
}
