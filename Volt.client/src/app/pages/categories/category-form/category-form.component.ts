import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CategoryClient, CategoryDto, CreateCategoryCommand, UpdateCategoryCommand } from '../../../core/services/clientAPI';
import { CityClient, CityDto, PagedResultOfCityDto } from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './category-form.component.html',
  styleUrls: ['./category-form.component.css', '../../../shared/styles/entity-form.css']
})
export class CategoryFormComponent implements OnInit {
  private readonly localeService = inject(LocaleService);

  categoryForm: FormGroup;
  isEditMode = false;
  categoryId: number | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  cities: CityDto[] = [];
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;

  constructor(
    private categoryClient: CategoryClient,
    private cityClient: CityClient,
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['', [Validators.required]],
      cityId: [null, [Validators.required]],
      imageUrl: [null]
    });
  }

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(city => ({
      value: city.cityId,
      label: city.name
    }));
  }

  ngOnInit(): void {
    this.loadCities();
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.categoryId = +id;
        this.isEditMode = true;
        this.loadCategory();
      } else {
        this.isEditMode = false;
        this.categoryId = null;
      }
    });
  }

  loadCities(): void {
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
      },
      error: (error) => {
        console.error('Error loading cities:', error);
      }
    });
  }

  loadCategory(): void {
    if (!this.categoryId) return;

    this.isLoading = true;
    this.categoryClient.getById(this.categoryId).subscribe({
      next: (category: CategoryDto) => {
        this.categoryForm.patchValue({
          name: category.name,
          description: category.description,
          cityId: category.cityId,
          imageUrl: category.imageUrl
        });

        if (category.imageUrl) {
          this.imagePreview = category.imageUrl;
        }

        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = this.localeService.translate('categories.failedToLoad');
        this.isLoading = false;
        console.error('Error loading category:', error);
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
        this.categoryForm.patchValue({ imageUrl: e.target.result });
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(): void {
    this.imagePreview = null;
    this.selectedImageFile = null;
    this.categoryForm.patchValue({ imageUrl: null });
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.categoryForm.value;

    if (this.isEditMode && this.categoryId) {
      const command = new UpdateCategoryCommand();
      command.categoryId = this.categoryId;
      command.name = formValue.name;
      command.description = formValue.description;
      command.cityId = formValue.cityId;
      command.imageUrl = this.selectedImageFile ? formValue.imageUrl : null;

      this.categoryClient.update(command).subscribe({
        next: () => {
          this.router.navigate(['/main/categories']);
        },
        error: (error: any) => {
          this.errorMessage =
            error.error?.detail ||
            error.error?.title ||
            this.localeService.translate('categories.failedToUpdate');
          this.isSaving = false;
          console.error('Error updating category:', error);
        }
      });
    } else {
      const command = new CreateCategoryCommand();
      command.name = formValue.name;
      command.description = formValue.description;
      command.cityId = formValue.cityId;
      command.imageUrl = formValue.imageUrl;

      this.categoryClient.create(command).subscribe({
        next: () => {
          this.router.navigate(['/main/categories']);
        },
        error: (error: any) => {
          this.errorMessage =
            error.error?.detail ||
            error.error?.title ||
            this.localeService.translate('categories.failedToCreate');
          this.isSaving = false;
          console.error('Error creating category:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/main/categories']);
  }
}
