import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  VehicleClient,
  VehicleDto,
  CreateVehicleCommand,
  UpdateVehicleCommand,
  SubCategoryClient,
  SubCategoryLookupDto,
  MerchantClient,
  MerchantLookupDto
} from '../../../core/services/clientAPI';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';
import { VehicleStatus } from '../../../core/enums/vehicle-status.enum';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-vehicle-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, MultiSelectComponent, TranslatePipe],
  templateUrl: './vehicle-form.component.html',
  styleUrls: ['./vehicle-form.component.css', '../../../shared/styles/entity-form.css']
})
export class VehicleFormComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  vehicleForm: FormGroup;
  isEditMode = false;
  vehicleId: number | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  subCategories: SubCategoryLookupDto[] = [];
  merchants: MerchantLookupDto[] = [];
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;

  get statusOptions(): MultiSelectOption[] {
    return [
      { value: VehicleStatus.Available, label: this.localeService.translate('vehicles.available') },
      { value: VehicleStatus.UnderMaintenance, label: this.localeService.translate('vehicles.maintenance') },
      { value: VehicleStatus.Rented, label: this.localeService.translate('vehicles.rented') }
    ];
  }

  constructor(
    private vehicleClient: VehicleClient,
    private subCategoryClient: SubCategoryClient,
    private merchantClient: MerchantClient,
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.vehicleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      vehicleCode: ['', [Validators.required, Validators.minLength(1)]],
      subCategoryId: [null, [Validators.required]],
      merchantId: [null, [Validators.required]],
      status: [VehicleStatus.Available, [Validators.required]],
      color: ['', [Validators.required]],
      type: ['', [Validators.required]],
      model: ['', [Validators.required]],
      price: [0, [Validators.required, Validators.min(0)]],
      speedKmh: [null],
      engineCapacityCc: [null],
      imageUrl: [null]
    });
  }

  get merchantOptions(): MultiSelectOption[] {
    return this.merchants
      .filter(merchant => merchant.merchantId != null)
      .map(merchant => ({
        value: merchant.merchantId as number,
        label: merchant.fullName || String(merchant.merchantId),
        description: merchant.mobileNumber || '—'
      }));
  }

  get subCategoryOptions(): MultiSelectOption[] {
    return this.subCategories.map(subCategory => ({
      value: subCategory.subCategoryId,
      label: `${subCategory.name} (${subCategory.categoryName})`
    }));
  }

  ngOnInit(): void {
    this.loadSubCategories();
    this.loadMerchants();
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.vehicleId = +id;
        this.isEditMode = true;
        this.loadVehicle();
      } else {
        this.isEditMode = false;
        this.vehicleId = null;
        this.applyDefaultSubCategoryFromQuery();
      }
    });
  }

  private applyDefaultSubCategoryFromQuery(): void {
    const subCategoryId = this.route.snapshot.queryParamMap.get('subCategoryId');
    if (subCategoryId) {
      this.vehicleForm.patchValue({ subCategoryId: +subCategoryId });
    }
  }

  loadSubCategories(): void {
    this.subCategoryClient.getLookup().subscribe({
      next: (result) => {
        this.subCategories = result || [];
      },
      error: (error) => {
        console.error('Error loading subcategories:', error);
      }
    });
  }

  loadMerchants(): void {
    this.merchantClient.getActive().subscribe({
      next: (result) => {
        this.merchants = result || [];
      },
      error: (error) => {
        console.error('Error loading merchants:', error);
      }
    });
  }

  private ensureMerchantOption(vehicle: VehicleDto): void {
    if (!vehicle.merchantId) {
      return;
    }

    const exists = this.merchants.some(m => m.merchantId === vehicle.merchantId);
    if (exists) {
      return;
    }

    const fallback = new MerchantLookupDto();
    fallback.merchantId = vehicle.merchantId;
    fallback.fullName = vehicle.merchantName || String(vehicle.merchantId);
    fallback.mobileNumber = '';
    this.merchants = [...this.merchants, fallback];
  }

  loadVehicle(): void {
    if (!this.vehicleId) return;

    this.isLoading = true;
    this.vehicleClient.getById(this.vehicleId).subscribe({
      next: (vehicle: VehicleDto) => {
        this.ensureMerchantOption(vehicle);
        this.vehicleForm.patchValue({
          name: vehicle.name,
          vehicleCode: vehicle.vehicleCode,
          subCategoryId: vehicle.subCategoryId,
          merchantId: vehicle.merchantId,
          status: vehicle.status as VehicleStatus,
          color: vehicle.color,
          type: vehicle.type,
          model: vehicle.model,
          price: vehicle.price,
          speedKmh: vehicle.speedKmh,
          engineCapacityCc: vehicle.engineCapacityCc,
          imageUrl: vehicle.imageUrl
        });

        if (vehicle.imageUrl) {
          this.imagePreview = vehicle.imageUrl;
        }

        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = this.localeService.translate('vehicles.failedToLoad');
        this.isLoading = false;
        console.error('Error loading vehicle:', error);
      }
    });
  }

  onImageSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.selectedImageFile = file;

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.imagePreview = e.target.result;
        this.vehicleForm.patchValue({ imageUrl: e.target.result });
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(): void {
    this.imagePreview = null;
    this.selectedImageFile = null;
    this.vehicleForm.patchValue({ imageUrl: null });
  }

  onSubmit(): void {
    if (this.vehicleForm.invalid) {
      this.vehicleForm.markAllAsTouched();
      return;
    }

    const merchantId = Number(this.vehicleForm.value.merchantId);
    if (!merchantId || merchantId <= 0) {
      this.vehicleForm.get('merchantId')?.setErrors({ required: true });
      this.vehicleForm.get('merchantId')?.markAsTouched();
      this.errorMessage = this.localeService.translate('vehicles.merchantRequired');
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.vehicleForm.value;

    if (this.isEditMode && this.vehicleId) {
      const command = new UpdateVehicleCommand();
      command.vehicleId = this.vehicleId;
      command.name = formValue.name;
      command.vehicleCode = formValue.vehicleCode;
      command.subCategoryId = formValue.subCategoryId;
      command.merchantId = merchantId;
      command.status = Number(formValue.status);
      command.color = formValue.color;
      command.type = formValue.type;
      command.model = formValue.model;
      command.price = Number(formValue.price);
      command.speedKmh = this.optionalInt(formValue.speedKmh);
      command.engineCapacityCc = this.optionalInt(formValue.engineCapacityCc);
      // Only send imageUrl if it's a new base64 image (starts with data:image/), otherwise send null
      command.imageUrl = this.selectedImageFile ? formValue.imageUrl : null;

      this.vehicleClient.update(command).subscribe({
        next: () => {
          this.router.navigate(['/main/vehicles']);
        },
        error: (error: any) => {
          this.errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('vehicles.failedToUpdate');
          this.isSaving = false;
          console.error('Error updating vehicle:', error);
        }
      });
    } else {
      const command = new CreateVehicleCommand();
      command.name = formValue.name;
      command.vehicleCode = formValue.vehicleCode;
      command.subCategoryId = formValue.subCategoryId;
      command.merchantId = merchantId;
      command.status = Number(formValue.status);
      command.color = formValue.color;
      command.type = formValue.type;
      command.model = formValue.model;
      command.price = Number(formValue.price);
      command.speedKmh = this.optionalInt(formValue.speedKmh);
      command.engineCapacityCc = this.optionalInt(formValue.engineCapacityCc);
      command.imageUrl = formValue.imageUrl;

      this.vehicleClient.create(command).subscribe({
        next: () => {
          this.router.navigate(['/main/vehicles']);
        },
        error: (error: any) => {
          this.errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('vehicles.failedToCreate');
          this.isSaving = false;
          console.error('Error creating vehicle:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/main/vehicles']);
  }

  private optionalInt(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }
}
