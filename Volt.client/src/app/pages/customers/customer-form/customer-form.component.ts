import { Component, OnInit, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  AdminCustomerClient,
  CustomerDto,
  AdminCreateCustomerCommand,
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
  selector: 'app-customer-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './customer-form.component.html',
  styleUrls: ['./customer-form.component.css', '../../../shared/styles/entity-form.css']
})
export class CustomerFormComponent implements OnInit {
  @ViewChild('personalImageInput', { static: false }) personalImageInputRef?: ElementRef<HTMLInputElement>;
  @ViewChild('commercialImageInput', { static: false }) commercialImageInputRef?: ElementRef<HTMLInputElement>;

  private readonly localeService = inject(LocaleService);

  customerForm: FormGroup;
  isEditMode = false;
  customerId: number | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  cities: CityDto[] = [];
  zones: ZoneLookupDto[] = [];
  isLoadingCities = false;

  personalImagePreview: string | null = null;
  selectedPersonalImage: File | null = null;
  commercialImagePreview: string | null = null;
  selectedCommercialImage: File | null = null;
  showCommercialImageError = false;

  constructor(
    private customerClient: AdminCustomerClient,
    private cityClient: CityClient,
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.customerForm = this.fb.group({
      mobileNumber: ['', [Validators.required, Validators.pattern(/^[0-9]+$/)]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      gender: ['', [Validators.required]],
      cityId: [null, [Validators.required]],
      zoneId: [null, [Validators.required]],
      email: [''],
      personalImage: [''],
      commercialRegisterImage: [''],
      registerAs: [0, [Validators.required]],
      verificationBy: [0, [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  get genderOptions(): MultiSelectOption[] {
    return [
      { value: 'Male', label: this.localeService.translate('common.male') },
      { value: 'Female', label: this.localeService.translate('common.female') }
    ];
  }

  get registerAsOptions(): MultiSelectOption[] {
    return [
      { value: 0, label: this.localeService.translate('common.individual') },
      { value: 1, label: this.localeService.translate('common.institution') }
    ];
  }

  get verificationByOptions(): MultiSelectOption[] {
    return [
      { value: 0, label: this.localeService.translate('common.phone') },
      { value: 1, label: this.localeService.translate('common.email') }
    ];
  }

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

  get isInstitution(): boolean {
    return this.customerForm.get('registerAs')?.value === 1;
  }

  ngOnInit(): void {
    this.loadCities();
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.customerId = +id;
        this.isEditMode = true;
        this.loadCustomer();
      } else {
        this.isEditMode = false;
        this.customerId = null;
      }
    });

    this.customerForm.get('verificationBy')?.valueChanges.subscribe(verificationBy => {
      const emailControl = this.customerForm.get('email');
      if (verificationBy === 1) {
        emailControl?.setValidators([Validators.required, Validators.email]);
      } else {
        emailControl?.setValidators([Validators.email]);
      }
      emailControl?.updateValueAndValidity();
    });

    this.customerForm.get('registerAs')?.valueChanges.subscribe(registerAs => {
      if (registerAs === 0) {
        if (this.selectedCommercialImage) {
          this.selectedCommercialImage = null;
          this.commercialImagePreview = null;
        }
        this.showCommercialImageError = false;
      }
    });

    this.customerForm.get('cityId')?.valueChanges.subscribe((cityId: number | null) => {
      this.loadZones(cityId);
    });
  }

  loadCities(): void {
    this.isLoadingCities = true;
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
        this.isLoadingCities = false;
      },
      error: (error) => {
        console.error('Error loading cities:', error);
        this.isLoadingCities = false;
      }
    });
  }

  loadZones(cityId: number | null, preferredZoneId: number | null = null): void {
    if (!cityId) {
      this.zones = [];
      this.customerForm.patchValue({ zoneId: null });
      return;
    }

    this.cityClient.getZonesByCity(cityId).subscribe({
      next: (zones) => {
        this.zones = zones || [];
        const keep = preferredZoneId ?? this.customerForm.get('zoneId')?.value;
        const next = this.zones.some(z => z.zoneId === keep) ? keep : null;
        this.customerForm.patchValue({ zoneId: next });
      },
      error: () => {
        this.zones = [];
        this.customerForm.patchValue({ zoneId: null });
      }
    });
  }

  loadCustomer(): void {
    if (!this.customerId) return;

    this.isLoading = true;
    this.customerClient.getById(this.customerId).subscribe({
      next: (customer: CustomerDto) => {
        this.customerForm.patchValue({
          mobileNumber: customer.mobileNumber,
          fullName: customer.fullName,
          gender: customer.gender,
          cityId: customer.cityId,
          zoneId: customer.zoneId,
          email: customer.email || '',
          registerAs: customer.registerAs || 0,
          verificationBy: customer.verificationBy || 0
        }, { emitEvent: false });
        this.loadZones(customer.cityId, customer.zoneId);

        if (customer.personalImage) {
          this.personalImagePreview = customer.personalImage;
        }

        if (customer.commercialRegisterImage) {
          this.commercialImagePreview = customer.commercialRegisterImage;
        }

        this.customerForm.get('password')?.clearValidators();
        this.customerForm.get('password')?.updateValueAndValidity();

        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('customers.failedToLoad');
        this.isLoading = false;
        console.error('Error loading customer:', error);
      }
    });
  }

  onPersonalImageSelect(): void {
    const input = this.personalImageInputRef?.nativeElement;
    if (input && input.files && input.files[0]) {
      const file = input.files[0];
      this.selectedPersonalImage = file;

      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target && e.target.result) {
          this.personalImagePreview = e.target.result as string;
        }
      };
      reader.readAsDataURL(file);
    }
  }

  onCommercialImageSelect(): void {
    const input = this.commercialImageInputRef?.nativeElement;
    if (input && input.files && input.files[0]) {
      const file = input.files[0];
      this.selectedCommercialImage = file;
      this.showCommercialImageError = false;

      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target && e.target.result) {
          this.commercialImagePreview = e.target.result as string;
        }
      };
      reader.readAsDataURL(file);
    }
  }

  removePersonalImage(): void {
    this.personalImagePreview = null;
    this.selectedPersonalImage = null;
    this.customerForm.patchValue({ personalImage: null });
  }

  removeCommercialImage(): void {
    this.commercialImagePreview = null;
    this.selectedCommercialImage = null;
    this.customerForm.patchValue({ commercialRegisterImage: null });
  }

  convertImageToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = error => reject(error);
    });
  }

  async onSubmit(): Promise<void> {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    if (this.isEditMode) {
      this.errorMessage = this.localeService.translate('customers.updateNotSupported');
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    const formValue = this.customerForm.getRawValue();

    const cityId = Number(formValue.cityId);
    const registerAs = Number(formValue.registerAs);
    const verificationBy = Number(formValue.verificationBy);

    if (!cityId || Number.isNaN(cityId)) {
      this.errorMessage = this.localeService.translate('customers.cityRequired');
      this.isSaving = false;
      return;
    }

    if (verificationBy === 1 && !formValue.email) {
      this.errorMessage = this.localeService.translate('customers.emailRequired');
      this.isSaving = false;
      return;
    }

    if (registerAs === 1 && !this.selectedCommercialImage && !this.commercialImagePreview) {
      this.showCommercialImageError = true;
      this.errorMessage = this.localeService.translate('customers.commercialImageRequired');
      this.isSaving = false;
      return;
    }
    this.showCommercialImageError = false;

    let personalImageBase64: string | null = null;
    let commercialRegisterImageBase64: string | null = null;

    if (this.selectedPersonalImage) {
      personalImageBase64 = await this.convertImageToBase64(this.selectedPersonalImage);
    }

    if (registerAs === 1 && this.selectedCommercialImage) {
      commercialRegisterImageBase64 = await this.convertImageToBase64(this.selectedCommercialImage);
    }

    const command = new AdminCreateCustomerCommand();
    command.mobileNumber = String(formValue.mobileNumber).trim();
    command.fullName = String(formValue.fullName).trim();
    command.gender = String(formValue.gender);
    command.cityId = cityId;
    command.zoneId = Number(formValue.zoneId);
    command.email = formValue.email ? String(formValue.email).trim() : null;
    command.personalImage = personalImageBase64;
    command.commercialRegisterImage = commercialRegisterImageBase64;
    command.registerAs = registerAs;
    command.verificationBy = verificationBy;
    command.password = String(formValue.password);

    this.customerClient.create(command).subscribe({
      next: () => {
        this.router.navigate(['/main/customers']);
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          error?.error?.detail ||
          error?.error?.title ||
          this.localeService.translate('customers.failedToCreate');
        this.isSaving = false;
        console.error('Error creating customer:', error);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/main/customers']);
  }
}
