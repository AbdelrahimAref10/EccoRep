import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  SubCategoryClient,
  SubCategoryDto,
  PagedResultOfSubCategoryDto,
  CategoryClient,
  CategoryLookupDto,
  CityClient,
  CityDto
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
  selector: 'app-subcategories',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ConfirmDialogComponent,
    PaginationComponent,
    MultiSelectComponent,
    TranslatePipe
  ],
  templateUrl: './subcategories.component.html',
  styleUrls: ['./subcategories.component.css', '../../shared/styles/list-filters.css', '../../shared/styles/entity-tiles.css']
})
export class SubCategoriesComponent implements OnInit {
  private readonly localeService = inject(LocaleService);

  subCategories: SubCategoryDto[] = [];
  categories: CategoryLookupDto[] = [];
  cities: CityDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  searchTerm = '';
  selectedCategoryIds: Array<string | number | boolean> = [];
  selectedCityIds: Array<string | number | boolean> = [];
  selectedStatusValues: Array<string | number | boolean> = [];
  selectedOfferValues: Array<string | number | boolean> = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  viewMode: 'table' | 'gallery' = this.readStoredViewMode();

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
    private subCategoryClient: SubCategoryClient,
    private categoryClient: CategoryClient,
    private cityClient: CityClient,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadLookups();

    this.route.queryParams.subscribe(params => {
      if (params['categoryId']) {
        this.selectedCategoryIds = [+params['categoryId']];
      }
      this.currentPage = 1;
      this.loadSubCategories();
    });
  }

  get categoryOptions(): MultiSelectOption[] {
    return this.categories.map(category => ({
      value: category.categoryId,
      label: category.name
    }));
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

  get offerOptions(): MultiSelectOption[] {
    return [
      { value: 'true', label: this.localeService.translate('subcategories.offer') },
      { value: 'false', label: this.localeService.translate('subcategories.regular') }
    ];
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.searchTerm.trim()) count++;
    count += this.selectedCategoryIds.length;
    count += this.selectedCityIds.length;
    count += this.selectedStatusValues.length;
    count += this.selectedOfferValues.length;
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

    for (const id of this.selectedCategoryIds) {
      const category = this.categories.find(c => c.categoryId === id);
      chips.push({
        key: `category-${id}`,
        label: category?.name || String(id),
        onRemove: () => {
          this.selectedCategoryIds = this.selectedCategoryIds.filter(v => v !== id);
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

    for (const value of this.selectedOfferValues) {
      chips.push({
        key: `offer-${value}`,
        label: value === 'true'
          ? this.localeService.translate('subcategories.offer')
          : this.localeService.translate('subcategories.regular'),
        onRemove: () => {
          this.selectedOfferValues = this.selectedOfferValues.filter(v => v !== value);
          this.applyFilters();
        }
      });
    }

    return chips;
  }

  loadLookups(): void {
    this.categoryClient.getLookup().subscribe({
      next: (result: CategoryLookupDto[]) => {
        this.categories = result || [];
      },
      error: (error: unknown) => console.error('Error loading categories:', error)
    });

    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result) => {
        this.cities = result.items || [];
      },
      error: (error: unknown) => console.error('Error loading cities:', error)
    });
  }

  loadSubCategories(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const categoryIds = this.selectedCategoryIds.map(Number);
    const cityIds = this.selectedCityIds.map(Number);
    const isActive = this.toNullableBool(this.selectedStatusValues);
    const isOffer = this.toNullableBool(this.selectedOfferValues);

    this.subCategoryClient.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      undefined,
      categoryIds.length ? categoryIds : undefined,
      cityIds.length ? cityIds : undefined,
      isActive,
      isOffer
    ).subscribe({
      next: (result: PagedResultOfSubCategoryDto) => {
        this.subCategories = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading subcategories:', error);
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadSubCategories();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategoryIds = [];
    this.selectedCityIds = [];
    this.selectedStatusValues = [];
    this.selectedOfferValues = [];
    this.applyFilters();
  }

  onCategoryIdsChange(values: Array<string | number | boolean>): void {
    this.selectedCategoryIds = values;
  }

  onCityIdsChange(values: Array<string | number | boolean>): void {
    this.selectedCityIds = values;
  }

  onStatusChange(values: Array<string | number | boolean>): void {
    this.selectedStatusValues = values;
  }

  onOfferChange(values: Array<string | number | boolean>): void {
    this.selectedOfferValues = values;
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadSubCategories();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  onDelete(subCategoryId: number): void {
    this.pendingDeleteId = subCategoryId;
    this.pendingDeactivateId = null;
    this.pendingAction = 'delete';
    this.confirmDialogTitle = this.localeService.translate('subcategories.deleteTitle');
    this.confirmDialogMessage = this.localeService.translate('subcategories.deleteMessage');
    this.confirmDialogType = 'danger';
    this.showConfirmDialog = true;
  }

  onDeactivate(subCategoryId: number): void {
    this.pendingDeactivateId = subCategoryId;
    this.pendingDeleteId = null;
    this.pendingActivateId = null;
    this.pendingAction = 'deactivate';
    this.confirmDialogTitle = this.localeService.translate('common.confirm');
    this.confirmDialogMessage = this.localeService.translate('common.areYouSure');
    this.confirmDialogType = 'warning';
    this.showConfirmDialog = true;
  }

  onActivate(subCategoryId: number): void {
    this.pendingActivateId = subCategoryId;
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
      this.subCategoryClient.delete(this.pendingDeleteId).subscribe({
        next: () => {
          this.showConfirmDialog = false;
          this.confirmDialogLoading = false;
          this.pendingDeleteId = null;
          this.pendingAction = null;
          this.showSuccessMessage(this.localeService.translate('common.deletedSuccessfully'));
          this.loadSubCategories();
        },
        error: (error: any) => {
          this.confirmDialogLoading = false;
          const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToDelete');
          this.showErrorMessage(errorMessage);
          this.showConfirmDialog = false;
          this.pendingDeleteId = null;
          this.pendingAction = null;
          console.error('Error deleting subcategory:', error);
        }
      });
    } else if (this.pendingAction === 'deactivate' && this.pendingDeactivateId !== null) {
      this.confirmDialogLoading = true;
      this.subCategoryClient.deactivate(this.pendingDeactivateId).subscribe({
        next: () => {
          this.showConfirmDialog = false;
          this.confirmDialogLoading = false;
          this.pendingDeactivateId = null;
          this.pendingAction = null;
          this.showSuccessMessage(this.localeService.translate('common.updatedSuccessfully'));
          this.loadSubCategories();
        },
        error: (error: any) => {
          this.confirmDialogLoading = false;
          const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToSave');
          this.showErrorMessage(errorMessage);
          this.showConfirmDialog = false;
          this.pendingDeactivateId = null;
          this.pendingAction = null;
          console.error('Error deactivating subcategory:', error);
        }
      });
    } else if (this.pendingAction === 'activate' && this.pendingActivateId !== null) {
      this.confirmDialogLoading = true;
      this.subCategoryClient.activate(this.pendingActivateId).subscribe({
        next: () => {
          this.showConfirmDialog = false;
          this.confirmDialogLoading = false;
          this.pendingActivateId = null;
          this.pendingAction = null;
          this.showSuccessMessage(this.localeService.translate('common.updatedSuccessfully'));
          this.loadSubCategories();
        },
        error: (error: any) => {
          this.confirmDialogLoading = false;
          const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToSave');
          this.showErrorMessage(errorMessage);
          this.showConfirmDialog = false;
          this.pendingActivateId = null;
          this.pendingAction = null;
          console.error('Error activating subcategory:', error);
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

  onViewVehicles(subCategoryId: number): void {
    this.router.navigate(['/main/vehicles'], { queryParams: { subCategoryId } });
  }

  onAddNew(): void {
    const queryParams: Record<string, number> = {};
    const fromRoute = this.route.snapshot.queryParamMap.get('categoryId');
    const categoryId = fromRoute
      ? +fromRoute
      : this.selectedCategoryIds.length === 1
        ? +this.selectedCategoryIds[0]
        : null;

    if (categoryId) {
      queryParams['categoryId'] = categoryId;
    }

    this.router.navigate(['/main/subcategories/new'], { queryParams });
  }

  onEdit(subCategory: SubCategoryDto): void {
    this.router.navigate(['/main/subcategories', subCategory.subCategoryId, 'edit']);
  }

  setViewMode(mode: 'table' | 'gallery'): void {
    if (this.viewMode === mode) {
      return;
    }
    this.viewMode = mode;
    localStorage.setItem('volt-subcategories-view', mode);
  }

  formatPrice(price: number): string {
    return `${Number(price || 0).toFixed(2)} ${this.localeService.translate('common.currency')}`;
  }

  private toNullableBool(values: Array<string | number | boolean>): boolean | undefined {
    if (values.length !== 1) {
      return undefined;
    }
    return String(values[0]) === 'true';
  }

  private readStoredViewMode(): 'table' | 'gallery' {
    const stored = localStorage.getItem('volt-subcategories-view');
    return stored === 'table' ? 'table' : 'gallery';
  }
}

