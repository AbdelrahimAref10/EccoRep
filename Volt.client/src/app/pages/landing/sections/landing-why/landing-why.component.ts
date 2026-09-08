import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { LpRevealDirective } from '../../../../shared/directives/lp-reveal.directive';

@Component({
  selector: 'app-landing-why',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LpRevealDirective],
  templateUrl: './landing-why.component.html',
  styleUrl: './landing-why.component.css'
})
export class LandingWhyComponent {
  readonly items = [
    { icon: 'eco', titleKey: 'landing.why.items.eco.title', descKey: 'landing.why.items.eco.desc' },
    { icon: 'affordable', titleKey: 'landing.why.items.affordable.title', descKey: 'landing.why.items.affordable.desc' },
    { icon: 'convenient', titleKey: 'landing.why.items.convenient.title', descKey: 'landing.why.items.convenient.desc' },
    { icon: 'smart', titleKey: 'landing.why.items.smart.title', descKey: 'landing.why.items.smart.desc' }
  ];
}
