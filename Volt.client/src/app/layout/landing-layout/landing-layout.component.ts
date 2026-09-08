import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-landing-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `<div class="lp-shell"><router-outlet /></div>`,
  styles: [`
    :host { display: block; min-height: 100vh; }
    .lp-shell { min-height: 100vh; background: hsl(var(--lp-background)); color: hsl(var(--lp-foreground)); }
  `]
})
export class LandingLayoutComponent {}
