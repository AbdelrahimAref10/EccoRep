import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SubCategoryClient, SubCategoryDto, CreateSubCategoryCommand, UpdateSubCategoryCommand } from '../../../core/services/clientAPI';
import { CategoryClient, CategoryLookupDto } from '../../../core/services/clientAPI';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-subcategory-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, MultiSelectComponent, TranslatePipe],
  templateUrl: './subcategory-form.component.html',
  styleUrls: ['./subcategory-form.component.css', '../../../shared/styles/entity-form.css']
})
export class SubCategoryFormComponent implements OnInit {
  subCategoryForm: FormGroup;
  isEditMode = false;
  subCategoryId: number | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  categories: CategoryLookupDto[] = [];
  imagePreview: string | null = null;
  selectedImageFile: File | null = null;

  constructor(
    private subCategoryClient: SubCategoryClient,
    private categoryClient: CategoryClient,
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.subCategoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['', [Validators.required]],
      categoryId: [null, [Validators.required]],
      isOffer: [false],
      imageUrl: [null]
    });
  }

  get categoryOptions(): MultiSelectOption[] {
    return this.categories.map(category => ({
      value: category.categoryId,
      label: category.name
    }));
  }

  ngOnInit(): void {
    this.loadCategories();
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.subCategoryId = +id;
        this.isEditMode = true;
        this.loadSubCategory();
      } else {
        this.isEditMode = false;
        this.subCategoryId = null;
        this.applyDefaultCategoryFromQuery();
      }
    });
  }

  private applyDefaultCategoryFromQuery(): void {
    const categoryId = this.route.snapshot.queryParamMap.get('categoryId');
    if (categoryId) {
      this.subCategoryForm.patchValue({ categoryId: +categoryId });
    }
  }

  loadCategories(): void {
    this.categoryClient.getLookup().subscribe({
      next: (result) => {
        this.categories = result || [];
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  loadSubCategory(): void {
    if (!this.subCategoryId) return;

    this.isLoading = true;
    this.subCategoryClient.getById(this.subCategoryId).subscribe({
      next: (subCategory: SubCategoryDto) => {
        this.subCategoryForm.patchValue({
          name: subCategory.name,
          description: subCategory.description,
          categoryId: subCategory.categoryId,
          isOffer: subCategory.isOffer || false,
          imageUrl: subCategory.imageUrl
        });

        if (subCategory.imageUrl) {
          this.imagePreview = subCategory.imageUrl;
        }

        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = 'Failed to load subcategory. Please try again.';
        this.isLoading = false;
        console.error('Error loading subcategory:', error);
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
        // Convert to base64 for backend
        const base64 = e.target.result.split(',')[1];
        this.subCategoryForm.patchValue({ imageUrl: `data:image/jpeg;base64,${base64}` });
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(): void {
    this.imagePreview = null;
    this.selectedImageFile = null;
    this.subCategoryForm.patchValue({ imageUrl: null });
  }

  onSubmit(): void {
    if (this.subCategoryForm.invalid) {
      this.subCategoryForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.subCategoryForm.value;

    if (this.isEditMode && this.subCategoryId) {
      const command = new UpdateSubCategoryCommand();
      command.subCategoryId = this.subCategoryId;
      command.name = formValue.name;
      command.description = formValue.description;
      command.categoryId = formValue.categoryId;
      command.isOffer = formValue.isOffer || false;
      // Only send imageUrl if it's a new base64 image (starts with data:image/), otherwise send null
      command.imageUrl = this.selectedImageFile ? formValue.imageUrl : null;

      this.subCategoryClient.update(command).subscribe({
        next: () => {
          this.router.navigate(['/main/subcategories']);
        },
        error: (error: any) => {
          this.errorMessage = error.error?.detail || error.error?.title || 'Failed to update subcategory. Please try again.';
          this.isSaving = false;
          console.error('Error updating subcategory:', error);
        }
      });
    } else {
      const command = new CreateSubCategoryCommand();
      command.name = formValue.name;
      command.description = formValue.description;
      command.categoryId = formValue.categoryId;
      command.isOffer = formValue.isOffer || false;
      command.imageUrl = formValue.imageUrl;

      this.subCategoryClient.create(command).subscribe({
        next: () => {
          this.router.navigate(['/main/subcategories']);
        },
        error: (error: any) => {
          this.errorMessage = error.error?.detail || error.error?.title || 'Failed to create subcategory. Please try again.';
          this.isSaving = false;
          console.error('Error creating subcategory:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/main/subcategories']);
  }
}


