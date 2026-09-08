import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  CategoryClient,
  CategoryDto,
  CityClient,
  CityDto,
  PagedResultOfCategoryDto
} from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ConfirmDialogComponent,
    PaginationComponent,
    MultiSelectComponent,
    TranslatePipe
  ],
  templateUrl: './categories.component.html',
  styleUrls: ['./categories.component.css', '../../shared/styles/list-filters.css', '../../shared/styles/entity-tiles.css']
})
export class CategoriesComponent implements OnInit {
  private readonly localeService = inject(LocaleService);

  categories: CategoryDto[] = [];
  cities: CityDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  viewMode: 'table' | 'gallery' = this.readStoredViewMode();

  searchTerm = '';
  selectedCityIds: Array<string | number | boolean> = [];
  selectedStatusValues: Array<string | number | boolean> = [];

  showConfirmDialog = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogType: 'danger' | 'warning' | 'info' = 'danger';
  confirmDialogLoading = false;
  pendingDeleteId: number | null = null;
  pendingDeactivateId: number | null = null;
  pendingActivateId: number | null = null;
  pendingAction: 'delete' | 'deactivate' | 'activate' | null = null;

  constructor(
    private categoryClient: CategoryClient,
    private cityClient: CityClient,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCities();
    this.loadCategories();
  }

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(city => ({
      value: city.cityId,
      label: city.name
    }));
  }

  get statusOptions(): MultiSelectOption[] {
    return [
      { value: 'true', label: this.localeService.translate('common.active') },
      { value: 'false', label: this.localeService.translate('common.inactive') }
    ];
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.searchTerm.trim()) count++;
    count += this.selectedCityIds.length;
    count += this.selectedStatusValues.length;
    return count;
  }

  get filterChips(): Array<{ key: string; label: string; onRemove: () => void }> {
    const chips: Array<{ key: string; label: string; onRemove: () => void }> = [];

    if (this.searchTerm.trim()) {
      chips.push({
        key: 'search',
        label: this.searchTerm.trim(),
        onRemove: () => {
          this.searchTerm = '';
          this.applyFilters();
        }
      });
    }

    for (const id of this.selectedCityIds) {
      const city = this.cities.find(c => c.cityId === id);
      chips.push({
        key: `city-${id}`,
        label: city?.name || String(id),
        onRemove: () => {
          this.selectedCityIds = this.selectedCityIds.filter(v => v !== id);
          this.applyFilters();
        }
      });
    }

    for (const value of this.selectedStatusValues) {
      chips.push({
        key: `status-${value}`,
        label: value === 'true'
          ? this.localeService.translate('common.active')
          : this.localeService.translate('common.inactive'),
        onRemove: () => {
          this.selectedStatusValues = this.selectedStatusValues.filter(v => v !== value);
          this.applyFilters();
        }
      });
    }

    return chips;
  }

  loadCities(): void {
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result) => {
        this.cities = result.items || [];
      },
      error: (error: unknown) => {
        console.error('Error loading cities:', error);
      }
    });
  }

  loadCategories(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const cityIds = this.selectedCityIds.map(Number);
    const isActive = this.toNullableBool(this.selectedStatusValues);

    this.categoryClient.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      cityIds.length ? cityIds : undefined,
      isActive
    ).subscribe({
      next: (result: PagedResultOfCategoryDto) => {
        this.categories = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading categories:', error);
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadCategories();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCityIds = [];
    this.selectedStatusValues = [];
    this.applyFilters();
  }

  onCityIdsChange(values: Array<string | number | boolean>): void {
    this.selectedCityIds = values;
  }

  onStatusChange(values: Array<string | number | boolean>): void {
    this.selectedStatusValues = values;
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadCategories();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  onDelete(categoryId: number): void {
    this.pendingDeleteId = categoryId;
    this.pendingDeactivateId = null;
    this.pendingAction = 'delete';
    this.confirmDialogTitle = this.localeService.translate('categories.deleteTitle');
    this.confirmDialogMessage = this.localeService.translate('categories.deleteMessage');
    this.confirmDialogType = 'danger';
    this.showConfirmDialog = true;
  }

  onDeactivate(categoryId: number): void {
    this.pendingDeactivateId = categoryId;
    this.pendingDeleteId = null;
    this.pendingActivateId = null;
    this.pendingAction = 'deactivate';
    this.confirmDialogTitle = this.localeService.translate('common.confirm');
    this.confirmDialogMessage = this.localeService.translate('common.areYouSure');
    this.confirmDialogType = 'warning';
    this.showConfirmDialog = true;
  }

  onActivate(categoryId: number): void {
    this.pendingActivateId = categoryId;
    this.pendingDeleteId = null;
    this.pendingDeactivateId = null;
    this.pendingAction = 'activate';
    this.confirmDialogTitle = this.localeService.translate('common.confirm');
    this.confirmDialogMessage = this.localeService.translate('common.areYouSure');
    this.confirmDialogType = 'info';
    this.showConfirmDialog = true;
  }

  onConfirmAction(): void {
    if (this.pendingAction === 'delete' && this.pendingDeleteId !== null) {
      this.confirmDialogLoading = true;
      this.categoryClient.delete(this.pendingDeleteId).subscribe({
        next: () => {
          this.showConfirmDialog = false;
          this.confirmDialogLoading = false;
          this.pendingDeleteId = null;
          this.pendingAction = null;
          this.showSuccessMessage(this.localeService.translate('common.deletedSuccessfully'));
          this.loadCategories();
        },
        error: (error: any) => {
          this.confirmDialogLoading = false;
          const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToDelete');
          this.showErrorMessage(errorMessage);
          this.showConfirmDialog = false;
          this.pendingDeleteId = null;
          this.pendingAction = null;
          console.error('Error deleting category:', error);
        }
      });
    } else if (this.pendingAction === 'deactivate' && this.pendingDeactivateId !== null) {
      this.confirmDialogLoading = true;
      this.categoryClient.deactivate(this.pendingDeactivateId).subscribe({
        next: () => {
          this.showConfirmDialog = false;
          this.confirmDialogLoading = false;
          this.pendingDeactivateId = null;
          this.pendingAction = null;
          this.showSuccessMessage(this.localeService.translate('common.updatedSuccessfully'));
          this.loadCategories();
        },
        error: (error: any) => {
          this.confirmDialogLoading = false;
          const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToSave');
          this.showErrorMessage(errorMessage);
          this.showConfirmDialog = false;
          this.pendingDeactivateId = null;
          this.pendingAction = null;
          console.error('Error deactivating category:', error);
        }
      });
    } else if (this.pendingAction === 'activate' && this.pendingActivateId !== null) {
      this.confirmDialogLoading = true;
      this.categoryClient.activate(this.pendingActivateId).subscribe({
        next: () => {
          this.showConfirmDialog = false;
          this.confirmDialogLoading = false;
          this.pendingActivateId = null;
          this.pendingAction = null;
          this.showSuccessMessage(this.localeService.translate('common.updatedSuccessfully'));
          this.loadCategories();
        },
        error: (error: any) => {
          this.confirmDialogLoading = false;
          const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToSave');
          this.showErrorMessage(errorMessage);
          this.showConfirmDialog = false;
          this.pendingActivateId = null;
          this.pendingAction = null;
          console.error('Error activating category:', error);
        }
      });
    }
  }

  onCancelAction(): void {
    this.showConfirmDialog = false;
    this.confirmDialogLoading = false;
    this.pendingDeleteId = null;
    this.pendingDeactivateId = null;
    this.pendingActivateId = null;
    this.pendingAction = null;
  }

  extractErrorMessage(error: any): string {
    if (!error) return '';
    if (typeof error === 'string') return error;
    // NSwag throws ProblemDetail directly on 400
    if (error.errorMessage) return error.errorMessage;
    if (error.detail) return error.detail;
    if (error.title) return error.title;
    if (error.error) {
      if (typeof error.error === 'string') return error.error;
      if (error.error.errorMessage) return error.error.errorMessage;
      if (error.error.detail) return error.error.detail;
      if (error.error.title) return error.error.title;
    }
    if (error.message && error.message !== 'A server side error occurred.') {
      return error.message;
    }
    return '';
  }

  showSuccessMessage(message: string): void {
    this.successMessage = message;
    this.errorMessage = '';
    setTimeout(() => {
      this.successMessage = '';
    }, 5000);
  }

  showErrorMessage(message: string): void {
    this.errorMessage = message;
    this.successMessage = '';
    setTimeout(() => {
      this.errorMessage = '';
    }, 5000);
  }

  onViewSubCategories(categoryId: number): void {
    this.router.navigate(['/main/subcategories'], { queryParams: { categoryId } });
  }

  onAddNew(): void {
    this.router.navigate(['/main/categories/new']);
  }

  onEdit(category: CategoryDto): void {
    this.router.navigate(['/main/categories', category.categoryId, 'edit']);
  }

  setViewMode(mode: 'table' | 'gallery'): void {
    if (this.viewMode === mode) {
      return;
    }
    this.viewMode = mode;
    localStorage.setItem('volt-categories-view', mode);
  }

  private toNullableBool(values: Array<string | number | boolean>): boolean | undefined {
    if (values.length !== 1) {
      return undefined;
    }
    return String(values[0]) === 'true';
  }

  private readStoredViewMode(): 'table' | 'gallery' {
    const stored = localStorage.getItem('volt-categories-view');
    return stored === 'table' ? 'table' : 'gallery';
  }
}
