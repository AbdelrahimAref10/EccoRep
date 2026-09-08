import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { LpRevealDirective } from '../../../../shared/directives/lp-reveal.directive';
import { LANDING_VEHICLES } from '../../vehicle-detail/landing-vehicles.data';

@Component({
  selector: 'app-landing-scooters',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LpRevealDirective],
  templateUrl: './landing-scooters.component.html',
  styleUrl: './landing-scooters.component.css'
})
export class LandingScootersComponent {
  private readonly router = inject(Router);

  readonly vehicles = LANDING_VEHICLES.map(v => ({
    slug: v.slug,
    image: v.cover,
    tone: v.tone,
    tagKey: v.tagKey,
    titleKey: v.titleKey,
    descKey: `landing.scooters.items.${v.slug}.desc`
  }));

  openVehicle(slug: string): void {
    void this.router.navigate(['/vehicles', slug]);
  }
}
