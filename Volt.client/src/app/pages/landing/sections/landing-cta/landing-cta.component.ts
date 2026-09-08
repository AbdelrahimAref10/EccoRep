import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { LpRevealDirective } from '../../../../shared/directives/lp-reveal.directive';

@Component({
  selector: 'app-landing-cta',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LpRevealDirective],
  templateUrl: './landing-cta.component.html',
  styleUrl: './landing-cta.component.css'
})
export class LandingCtaComponent {}
