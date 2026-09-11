import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  AdminCreateDeliveryCommand,
  AdminDeliveryClient,
  AdminUpdateDeliveryCommand,
  CityClient,
  CityDto,
  PagedResultOfCityDto
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-delivery-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './delivery-form.component.html',
  styleUrls: ['./delivery-form.component.css', '../../../shared/styles/entity-form.css']
})
export class DeliveryFormComponent implements OnInit {
  @ViewChild('personalImageInput', { static: false }) personalImageInputRef?: ElementRef<HTMLInputElement>;

  private readonly localeService = inject(LocaleService);
  private readonly deliveryClient = inject(AdminDeliveryClient);
  private readonly cityClient = inject(CityClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  deliveryForm!: FormGroup;
  isEditMode = false;
  deliveryId: number | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  personalImagePreview: string | null = null;
  existingPersonalImage: string | null = null;
  cities: CityDto[] = [];

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(city => ({
      value: city.cityId,
      label: city.name
    }));
  }

  ngOnInit(): void {
    this.deliveryForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      mobileNumber: ['', [Validators.required, Validators.pattern(/^[0-9]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.minLength(6)]],
      cityId: [null, [Validators.required]],
      personalImage: [''],
      isActive: [true]
    });

    this.loadCities();

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.deliveryId = Number(idParam);
      this.deliveryForm.get('mobileNumber')?.disable();
      this.deliveryForm.get('password')?.clearValidators();
      this.deliveryForm.get('password')?.updateValueAndValidity();
      this.loadDelivery(this.deliveryId);
    } else {
      this.deliveryForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
      this.deliveryForm.get('password')?.updateValueAndValidity();
    }
  }

  loadCities(): void {
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
      },
      error: () => {
        this.errorMessage = this.localeService.translate('deliveries.loadFailed');
      }
    });
  }

  loadDelivery(id: number): void {
    this.isLoading = true;
    this.deliveryClient.getById(id).subscribe({
      next: (delivery) => {
        this.existingPersonalImage = delivery.personalImage || null;
        this.personalImagePreview = delivery.personalImage || null;
        this.deliveryForm.patchValue({
          userName: delivery.userName || '',
          fullName: delivery.fullName,
          mobileNumber: delivery.mobileNumber,
          email: delivery.email || '',
          cityId: delivery.cityId || null,
          isActive: delivery.isActive,
          personalImage: ''
        });
        this.isLoading = false;
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('deliveries.loadFailed');
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
      this.deliveryForm.patchValue({ personalImage: result });
    };
    reader.readAsDataURL(file);
  }

  removePersonalImage(): void {
    this.personalImagePreview = null;
    this.existingPersonalImage = null;
    this.deliveryForm.patchValue({ personalImage: '' });
    if (this.personalImageInputRef?.nativeElement) {
      this.personalImageInputRef.nativeElement.value = '';
    }
  }

  onCancel(): void {
    this.router.navigate(['/main/deliveries']);
  }

  onSubmit(): void {
    if (this.deliveryForm.invalid) {
      this.deliveryForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    const value = this.deliveryForm.getRawValue();
    const cityId = Number(value.cityId);

    if (this.isEditMode && this.deliveryId) {
      const command = new AdminUpdateDeliveryCommand();
      command.deliveryId = this.deliveryId;
      command.userName = value.userName;
      command.fullName = value.fullName;
      command.email = value.email;
      command.cityId = cityId;
      command.personalImage = value.personalImage || this.existingPersonalImage || null;
      command.isActive = !!value.isActive;
      command.password = value.password || null;

      this.deliveryClient.update(this.deliveryId, command).subscribe({
        next: () => this.router.navigate(['/main/deliveries']),
        error: (error: any) => {
          this.isSaving = false;
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('deliveries.updateFailed');
        }
      });
      return;
    }

    const create = new AdminCreateDeliveryCommand();
    create.userName = value.userName;
    create.fullName = value.fullName;
    create.mobileNumber = value.mobileNumber;
    create.email = value.email;
    create.password = value.password;
    create.cityId = cityId;
    create.personalImage = value.personalImage || null;
    create.isActive = !!value.isActive;

    this.deliveryClient.create(create).subscribe({
      next: () => this.router.navigate(['/main/deliveries']),
      error: (error: any) => {
        this.isSaving = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('deliveries.createFailed');
      }
    });
  }
}
