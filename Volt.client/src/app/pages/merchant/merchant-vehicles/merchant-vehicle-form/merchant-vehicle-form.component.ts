import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  CategoryLookupDto,
  MerchantClient,
  MerchantCreateVehicleCommand,
  MerchantUpdateVehicleCommand,
  SubCategoryDto,
  VehicleDto
} from '../../../../core/services/clientAPI';
import { VehicleStatus } from '../../../../core/enums/vehicle-status.enum';
import { LocaleService } from '../../../../core/services/locale.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-vehicle-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslatePipe],
  templateUrl: './merchant-vehicle-form.component.html',
  styleUrls: ['./merchant-vehicle-form.component.css', '../../../../shared/styles/entity-form.css']
})
export class MerchantVehicleFormComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  vehicleForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    vehicleCode: ['', [Validators.required, Validators.minLength(1)]],
    categoryId: [null as number | null, [Validators.required]],
    subCategoryId: [null as number | null, [Validators.required]],
    status: [VehicleStatus.Available, [Validators.required]],
    imageUrl: [null as string | null]
  });

  categories: CategoryLookupDto[] = [];
  subCategories: SubCategoryDto[] = [];
  isEditMode = false;
  vehicleId: number | null = null;
  isLoading = false;
  isSaving = false;
  isLoadingSubs = false;
  errorMessage = '';
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;

  readonly statusOptions = [
    { value: VehicleStatus.Available, key: 'vehicles.available' },
    { value: VehicleStatus.UnderMaintenance, key: 'vehicles.maintenance' },
    { value: VehicleStatus.Rented, key: 'vehicles.rented' }
  ];

  ngOnInit(): void {
    this.loadCategories();
    this.vehicleForm.get('categoryId')?.valueChanges.subscribe((categoryId: number | null) => {
      this.onCategorySelected(categoryId, true);
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.vehicleId = +id;
        this.isEditMode = true;
        this.loadVehicle();
      } else {
        this.isEditMode = false;
        this.vehicleId = null;
      }
    });
  }

  loadCategories(): void {
    this.merchantClient.getCategories().subscribe({
      next: (list) => (this.categories = list || []),
      error: () => (this.categories = [])
    });
  }

  onCategorySelected(categoryId: number | null, resetSub = true): void {
    if (resetSub) {
      this.vehicleForm.patchValue({ subCategoryId: null }, { emitEvent: false });
    }
    this.subCategories = [];
    if (!categoryId) {
      return;
    }
    this.isLoadingSubs = true;
    this.merchantClient.getSubCategoriesByCategory(categoryId).subscribe({
      next: (list) => {
        this.subCategories = list || [];
        this.isLoadingSubs = false;
      },
      error: () => {
        this.subCategories = [];
        this.isLoadingSubs = false;
      }
    });
  }

  loadVehicle(): void {
    if (!this.vehicleId) return;
    this.isLoading = true;
    this.merchantClient.getMyVehicle(this.vehicleId).subscribe({
      next: (vehicle: VehicleDto) => {
        this.vehicleForm.patchValue(
          {
            name: vehicle.name,
            vehicleCode: vehicle.vehicleCode,
            categoryId: vehicle.categoryId,
            subCategoryId: vehicle.subCategoryId,
            status: vehicle.status as VehicleStatus,
            imageUrl: vehicle.imageUrl
          },
          { emitEvent: false }
        );
        this.onCategorySelected(vehicle.categoryId, false);
        if (vehicle.imageUrl) {
          this.imagePreview = vehicle.imageUrl;
        }
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('vehicles.failedToLoad');
        this.isLoading = false;
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

    this.isSaving = true;
    this.errorMessage = '';
    const formValue = this.vehicleForm.value;

    if (this.isEditMode && this.vehicleId) {
      const command = new MerchantUpdateVehicleCommand();
      command.vehicleId = this.vehicleId;
      command.name = formValue.name;
      command.vehicleCode = formValue.vehicleCode;
      command.subCategoryId = formValue.subCategoryId;
      command.status = Number(formValue.status);
      command.imageUrl = this.selectedImageFile ? formValue.imageUrl : null;

      this.merchantClient.updateVehicle(command).subscribe({
        next: () => this.router.navigate(['/merchant/vehicles']),
        error: (error: any) => {
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('vehicles.failedToUpdate');
          this.isSaving = false;
        }
      });
    } else {
      const command = new MerchantCreateVehicleCommand();
      command.name = formValue.name;
      command.vehicleCode = formValue.vehicleCode;
      command.subCategoryId = formValue.subCategoryId;
      command.status = Number(formValue.status);
      command.imageUrl = formValue.imageUrl;

      this.merchantClient.createVehicle(command).subscribe({
        next: () => this.router.navigate(['/merchant/vehicles']),
        error: (error: any) => {
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('vehicles.failedToCreate');
          this.isSaving = false;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/merchant/vehicles']);
  }
}
