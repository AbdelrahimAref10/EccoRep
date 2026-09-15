import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  CityClient,
  CityDto,
  UpdateZoneDeliveryRatesCommand,
  ZoneDeliveryMatrixDto,
  ZoneDeliveryRateItem,
  ZoneLookupDto
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

interface RateRow {
  toZoneId: number;
  toZoneName: string;
  fee: number;
  isSameZone: boolean;
}

@Component({
  selector: 'app-city-zone-rates',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './city-zone-rates.component.html',
  styleUrls: ['./city-zone-rates.component.css', '../../../shared/styles/entity-form.css']
})
export class CityZoneRatesComponent implements OnInit {
  private readonly cityClient = inject(CityClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  cityId = 0;
  city: CityDto | null = null;
  zones: ZoneLookupDto[] = [];
  fromZoneId: number | null = null;
  rows: RateRow[] = [];
  isLoading = true;
  isLoadingMatrix = false;
  isSaving = false;
  isDirty = false;
  errorMessage = '';
  successMessage = '';

  get fromZoneName(): string {
    return this.zones.find(z => z.zoneId === this.fromZoneId)?.name || '';
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.router.navigate(['/main/cities']);
      return;
    }
    this.cityId = id;
    this.bootstrap();
  }

  onBack(): void {
    this.router.navigate(['/main/cities']);
  }

  selectFromZone(zoneId: number): void {
    if (this.fromZoneId === zoneId || this.isSaving) return;
    if (this.isDirty && !window.confirm(this.localeService.translate('cities.discardUnsavedRates'))) {
      return;
    }
    this.fromZoneId = zoneId;
    this.loadMatrix();
  }

  onFeeChange(): void {
    this.isDirty = true;
    this.successMessage = '';
  }

  save(): void {
    if (!this.fromZoneId || this.rows.length === 0) return;

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const command = new UpdateZoneDeliveryRatesCommand();
    command.fromZoneId = this.fromZoneId;
    command.rates = this.rows.map(row => {
      const item = new ZoneDeliveryRateItem();
      item.toZoneId = row.toZoneId;
      item.fee = Number(row.fee) || 0;
      return item;
    });

    this.cityClient.updateDeliveryRates(command).subscribe({
      next: () => {
        this.isSaving = false;
        this.isDirty = false;
        this.successMessage = this.localeService.translate('cities.ratesSaved');
        setTimeout(() => { this.successMessage = ''; }, 3500);
      },
      error: (error: any) => {
        this.isSaving = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          error?.error?.detail ||
          this.localeService.translate('cities.failedToUpdate');
      }
    });
  }

  private bootstrap(): void {
    this.cityClient.getById(this.cityId).subscribe({
      next: (city) => {
        this.city = city;
        if (!city.zoneGroupId) {
          this.isLoading = false;
          return;
        }
        this.cityClient.getZonesByCity(this.cityId).subscribe({
          next: (zones) => {
            this.zones = zones || [];
            this.fromZoneId = this.zones[0]?.zoneId ?? null;
            this.isLoading = false;
            if (this.fromZoneId) this.loadMatrix();
          },
          error: () => {
            this.isLoading = false;
            this.errorMessage = this.localeService.translate('cities.failedToLoad');
          }
        });
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = this.localeService.translate('cities.failedToLoad');
      }
    });
  }

  private loadMatrix(): void {
    if (!this.fromZoneId) return;
    this.isLoadingMatrix = true;
    this.cityClient.getDeliveryMatrix(this.fromZoneId).subscribe({
      next: (matrix: ZoneDeliveryMatrixDto) => {
        this.rows = (matrix.rates || []).map(rate => ({
          toZoneId: rate.toZoneId,
          toZoneName: rate.toZoneName,
          fee: rate.fee ?? 0,
          isSameZone: rate.toZoneId === matrix.fromZoneId
        }));
        this.isDirty = false;
        this.isLoadingMatrix = false;
      },
      error: () => {
        this.isLoadingMatrix = false;
        this.errorMessage = this.localeService.translate('cities.failedToLoad');
      }
    });
  }
}
