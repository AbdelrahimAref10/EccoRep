import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { LpRevealDirective } from '../../../../shared/directives/lp-reveal.directive';

@Component({
  selector: 'app-landing-features',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LpRevealDirective],
  templateUrl: './landing-features.component.html',
  styleUrl: './landing-features.component.css'
})
export class LandingFeaturesComponent {
  readonly features = [
    { icon: 'booking', titleKey: 'landing.features.items.booking.title', descKey: 'landing.features.items.booking.desc' },
    { icon: 'vehicles', titleKey: 'landing.features.items.vehicles.title', descKey: 'landing.features.items.vehicles.desc' },
    { icon: 'flexible', titleKey: 'landing.features.items.flexible.title', descKey: 'landing.features.items.flexible.desc' },
    { icon: 'payments', titleKey: 'landing.features.items.payments.title', descKey: 'landing.features.items.payments.desc' },
    { icon: 'notify', titleKey: 'landing.features.items.notify.title', descKey: 'landing.features.items.notify.desc' },
    { icon: 'profile', titleKey: 'landing.features.items.profile.title', descKey: 'landing.features.items.profile.desc' }
  ];
}
