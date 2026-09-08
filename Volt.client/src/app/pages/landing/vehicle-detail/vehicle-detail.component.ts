import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ThemeService } from '../../../core/services/theme.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { LpRevealDirective } from '../../../shared/directives/lp-reveal.directive';
import { LandingHeaderComponent } from '../sections/landing-header/landing-header.component';
import { LandingFooterComponent } from '../sections/landing-footer/landing-footer.component';
import {
  LANDING_VEHICLES,
  LandingVehicle,
  getLandingVehicle
} from './landing-vehicles.data';

@Component({
  selector: 'app-vehicle-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    TranslatePipe,
    LpRevealDirective,
    LandingHeaderComponent,
    LandingFooterComponent
  ],
  templateUrl: './vehicle-detail.component.html',
  styleUrl: './vehicle-detail.component.css'
})
export class VehicleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly theme = inject(ThemeService);

  readonly vehicle = signal<LandingVehicle | null>(null);
  readonly activeImage = signal('');
  readonly related = computed(() => {
    const current = this.vehicle();
    if (!current) return [];
    return LANDING_VEHICLES.filter(v => v.slug !== current.slug);
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const slug = params.get('slug') ?? '';
      const found = getLandingVehicle(slug);
      if (!found) {
        void this.router.navigateByUrl('/');
        return;
      }
      this.vehicle.set(found);
      this.activeImage.set(found.gallery[0] ?? found.cover);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });
  }

  selectImage(src: string): void {
    this.activeImage.set(src);
  }

  goBack(): void {
    void this.router.navigate(['/'], { fragment: 'vehicles' });
  }
}
