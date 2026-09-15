import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  AdminCreateMerchantCommand,
  AdminMerchantClient,
  AdminUpdateMerchantCommand,
  CityClient,
  CityDto,
  PagedResultOfCityDto,
  ZoneLookupDto
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-merchant-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './merchant-form.component.html',
  styleUrls: ['./merchant-form.component.css', '../../../shared/styles/entity-form.css']
})
export class MerchantFormComponent implements OnInit {
  @ViewChild('personalImageInput', { static: false }) personalImageInputRef?: ElementRef<HTMLInputElement>;

  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(AdminMerchantClient);
  private readonly cityClient = inject(CityClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  merchantForm!: FormGroup;
  isEditMode = false;
  merchantId: number | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  personalImagePreview: string | null = null;
  existingPersonalImage: string | null = null;
  cities: CityDto[] = [];
  zones: ZoneLookupDto[] = [];

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(city => ({
      value: city.cityId,
      label: city.name
    }));
  }

  get zoneOptions(): MultiSelectOption[] {
    return this.zones.map(zone => ({
      value: zone.zoneId,
      label: zone.name
    }));
  }

  ngOnInit(): void {
    this.merchantForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      mobileNumber: ['', [Validators.required, Validators.pattern(/^[0-9]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.minLength(6)]],
      cityId: [null, [Validators.required]],
      zoneId: [null, [Validators.required]],
      personalImage: [''],
      isActive: [true],
      cashOnReceive: [false]
    });

    this.loadCities();
    this.merchantForm.get('cityId')?.valueChanges.subscribe((cityId: number | null) => {
      this.loadZones(cityId);
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.merchantId = Number(idParam);
      this.merchantForm.get('mobileNumber')?.disable();
      this.merchantForm.get('password')?.clearValidators();
      this.merchantForm.get('password')?.updateValueAndValidity();
      this.loadMerchant(this.merchantId);
    } else {
      this.merchantForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
      this.merchantForm.get('password')?.updateValueAndValidity();
    }
  }

  loadCities(): void {
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
      },
      error: () => {
        this.errorMessage = this.localeService.translate('merchants.loadFailed');
      }
    });
  }

  loadZones(cityId: number | null, preferredZoneId: number | null = null): void {
    if (!cityId) {
      this.zones = [];
      this.merchantForm.patchValue({ zoneId: null });
      return;
    }

    this.cityClient.getZonesByCity(cityId).subscribe({
      next: (zones) => {
        this.zones = zones || [];
        const keep = preferredZoneId ?? this.merchantForm.get('zoneId')?.value;
        const next = this.zones.some(z => z.zoneId === keep) ? keep : null;
        this.merchantForm.patchValue({ zoneId: next });
      },
      error: () => {
        this.zones = [];
        this.merchantForm.patchValue({ zoneId: null });
      }
    });
  }

  loadMerchant(id: number): void {
    this.isLoading = true;
    this.merchantClient.getById(id).subscribe({
      next: (merchant) => {
        this.existingPersonalImage = merchant.personalImage || null;
        this.personalImagePreview = merchant.personalImage || null;
        this.merchantForm.patchValue({
          userName: merchant.userName || '',
          fullName: merchant.fullName,
          mobileNumber: merchant.mobileNumber,
          email: merchant.email || '',
          cityId: merchant.cityId || null,
          zoneId: merchant.zoneId || null,
          isActive: merchant.isActive,
          cashOnReceive: merchant.cashOnReceive,
          personalImage: ''
        }, { emitEvent: false });
        this.loadZones(merchant.cityId || null, merchant.zoneId || null);
        this.isLoading = false;
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchants.loadFailed');
      }
    });
  }

  onPersonalImageSelect(): void {
    const input = this.personalImageInputRef?.nativeElement;
    const file = input?.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result || '');
      this.personalImagePreview = result;
      this.merchantForm.patchValue({ personalImage: result });
    };
    reader.readAsDataURL(file);
  }

  removePersonalImage(): void {
    this.personalImagePreview = this.isEditMode ? null : null;
    this.existingPersonalImage = null;
    this.merchantForm.patchValue({ personalImage: '' });
    if (this.personalImageInputRef?.nativeElement) {
      this.personalImageInputRef.nativeElement.value = '';
    }
  }

  onCancel(): void {
    this.router.navigate(['/main/merchants']);
  }

  onSubmit(): void {
    if (this.merchantForm.invalid) {
      this.merchantForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    const value = this.merchantForm.getRawValue();
    const cityId = Number(value.cityId);

    if (this.isEditMode && this.merchantId) {
      const command = new AdminUpdateMerchantCommand();
      command.merchantId = this.merchantId;
      command.userName = value.userName;
      command.fullName = value.fullName;
      command.email = value.email;
      command.cityId = cityId;
      command.zoneId = Number(value.zoneId);
      command.personalImage = value.personalImage || this.existingPersonalImage || null;
      command.isActive = !!value.isActive;
      command.cashOnReceive = !!value.cashOnReceive;
      command.password = value.password || null;

      this.merchantClient.update(this.merchantId, command).subscribe({
        next: () => this.router.navigate(['/main/merchants']),
        error: (error: any) => {
          this.isSaving = false;
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('merchants.updateFailed');
        }
      });
      return;
    }

    const create = new AdminCreateMerchantCommand();
    create.userName = value.userName;
    create.fullName = value.fullName;
    create.mobileNumber = value.mobileNumber;
    create.email = value.email;
    create.password = value.password;
    create.cityId = cityId;
    create.zoneId = Number(value.zoneId);
    create.personalImage = value.personalImage || null;
    create.isActive = !!value.isActive;
    create.cashOnReceive = !!value.cashOnReceive;

    this.merchantClient.create(create).subscribe({
      next: () => this.router.navigate(['/main/merchants']),
      error: (error: any) => {
        this.isSaving = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchants.createFailed');
      }
    });
  }
}
