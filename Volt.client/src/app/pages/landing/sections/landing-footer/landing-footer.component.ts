import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { LpRevealDirective } from '../../../../shared/directives/lp-reveal.directive';

@Component({
  selector: 'app-landing-footer',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LpRevealDirective],
  templateUrl: './landing-footer.component.html',
  styleUrl: './landing-footer.component.css'
})
export class LandingFooterComponent {
  readonly year = new Date().getFullYear();
}
