import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-home',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <section class="merchant-home">
      <h1>{{ 'merchant.homeTitle' | t }}</h1>
      <p>{{ 'merchant.homePlaceholder' | t }}</p>
    </section>
  `,
  styles: [`
    .merchant-home {
      max-width: 40rem;
    }
    .merchant-home h1 {
      margin: 0 0 0.5rem;
      font-size: 1.5rem;
    }
    .merchant-home p {
      margin: 0;
      color: #4b5563;
    }
  `]
})
export class MerchantHomeComponent {}
