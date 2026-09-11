import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  VehicleClient,
  VehicleDto,
  PagedResultOfVehicleDto,
  SubCategoryClient,
  SubCategoryLookupDto,
  CategoryClient,
  CategoryLookupDto,
  CityClient,
  CityDto
} from '../../core/services/clientAPI';
import { VehicleStatus } from '../../core/enums/vehicle-status.enum';
import { LocaleService } from '../../core/services/locale.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-vehicles',
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
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.css', '../../shared/styles/list-filters.css', '../../shared/styles/entity-tiles.css']
})
export class VehiclesComponent implements OnInit {
  private readonly localeService = inject(LocaleService);

  vehicles: VehicleDto[] = [];
  categories: CategoryLookupDto[] = [];
  subCategories: SubCategoryLookupDto[] = [];
  cities: CityDto[] = [];

  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  totalPages = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  viewMode: 'table' | 'gallery' = this.readStoredViewMode();

  searchTerm = '';
  selectedCategoryIds: Array<string | number | boolean> = [];
  selectedSubCategoryIds: Array<string | number | boolean> = [];
  selectedCityIds: Array<string | number | boolean> = [];
  selectedStatuses: Array<string | number | boolean> = [];

  showConfirmDialog = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogType: 'danger' | 'warning' | 'info' = 'danger';
  confirmDialogLoading = false;
  pendingDeleteId: number | null = null;

  constructor(
    private vehicleClient: VehicleClient,
    private categoryClient: CategoryClient,
    private subCategoryClient: SubCategoryClient,
    private cityClient: CityClient,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadLookups();

    this.route.queryParams.subscribe(params => {
      if (params['subCategoryId']) {
        this.selectedSubCategoryIds = [+params['subCategoryId']];
      }
      this.currentPage = 1;
      this.loadVehicles();
    });
  }

  get categoryOptions(): MultiSelectOption[] {
    return this.categories.map(category => ({
      value: category.categoryId,
      label: category.name
    }));
  }

  get subCategoryOptions(): MultiSelectOption[] {
    return this.subCategories.map(subCategory => ({
      value: subCategory.subCategoryId,
      label: subCategory.name
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
      { value: VehicleStatus.Available, label: this.localeService.translate('vehicles.available') },
      { value: VehicleStatus.UnderMaintenance, label: this.localeService.translate('vehicles.maintenance') },
      { value: VehicleStatus.Rented, label: this.localeService.translate('vehicles.rented') }
    ];
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.searchTerm.trim()) count++;
    count += this.selectedCategoryIds.length;
    count += this.selectedSubCategoryIds.length;
    count += this.selectedCityIds.length;
    count += this.selectedStatuses.length;
    return count;
  }

  clearStatusFilters(): void {
    this.selectedStatuses = [];
    this.applyFilters();
  }

  isStatusSelected(status: string | number | boolean): boolean {
    return this.selectedStatuses.map(String).includes(String(status));
  }

  toggleStatusChip(status: string | number | boolean): void {
    const value = String(status);
    if (this.isStatusSelected(value)) {
      this.selectedStatuses = this.selectedStatuses.filter(v => String(v) !== value);
    } else {
      this.selectedStatuses = [...this.selectedStatuses, value];
    }
    this.applyFilters();
  }

  loadLookups(): void {
    this.categoryClient.getLookup().subscribe({
      next: (result) => {
        this.categories = result || [];
      },
      error: (error: unknown) => console.error('Error loading categories:', error)
    });

    this.subCategoryClient.getLookup().subscribe({
      next: (result) => {
        this.subCategories = result || [];
      },
      error: (error: unknown) => console.error('Error loading subcategories:', error)
    });

    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result) => {
        this.cities = result.items || [];
      },
      error: (error: unknown) => console.error('Error loading cities:', error)
    });
  }

  loadVehicles(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const categoryIds = this.selectedCategoryIds.map(Number);
    const subCategoryIds = this.selectedSubCategoryIds.map(Number);
    const cityIds = this.selectedCityIds.map(Number);
    const statuses = this.selectedStatuses
      .map(v => Number(v))
      .filter(v => !Number.isNaN(v));

    this.vehicleClient.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      undefined,
      categoryIds.length ? categoryIds : undefined,
      undefined,
      subCategoryIds.length ? subCategoryIds : undefined,
      cityIds.length ? cityIds : undefined,
      undefined,
      undefined,
      statuses.length ? statuses : undefined
    ).subscribe({
      next: (result: PagedResultOfVehicleDto) => {
        this.vehicles = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading vehicles:', error);
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadVehicles();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategoryIds = [];
    this.selectedSubCategoryIds = [];
    this.selectedCityIds = [];
    this.selectedStatuses = [];
    this.applyFilters();
  }

  onCategoryIdsChange(values: Array<string | number | boolean>): void {
    this.selectedCategoryIds = values;
  }

  onSubCategoryIdsChange(values: Array<string | number | boolean>): void {
    this.selectedSubCategoryIds = values;
  }

  onCityIdsChange(values: Array<string | number | boolean>): void {
    this.selectedCityIds = values;
  }

  onStatusesChange(values: Array<string | number | boolean>): void {
    this.selectedStatuses = values;
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadVehicles();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  onAddNew(): void {
    const queryParams: Record<string, number> = {};
    if (this.selectedSubCategoryIds.length === 1) {
      queryParams['subCategoryId'] = Number(this.selectedSubCategoryIds[0]);
    }
    this.router.navigate(['/main/vehicles/new'], { queryParams });
  }

  onEdit(vehicle: VehicleDto): void {
    this.router.navigate(['/main/vehicles', vehicle.vehicleId, 'edit']);
  }

  onDelete(vehicleId: number): void {
    this.pendingDeleteId = vehicleId;
    this.confirmDialogTitle = this.localeService.translate('vehicles.deleteTitle');
    this.confirmDialogMessage = this.localeService.translate('vehicles.deleteMessage');
    this.confirmDialogType = 'danger';
    this.showConfirmDialog = true;
  }

  onConfirmDelete(): void {
    if (this.pendingDeleteId === null) return;

    this.confirmDialogLoading = true;
    this.vehicleClient.delete(this.pendingDeleteId).subscribe({
      next: () => {
        this.showConfirmDialog = false;
        this.confirmDialogLoading = false;
        this.pendingDeleteId = null;
        this.showSuccessMessage(this.localeService.translate('common.deletedSuccessfully'));
        this.loadVehicles();
      },
      error: (error: any) => {
        this.confirmDialogLoading = false;
        const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToDelete');
        this.showErrorMessage(errorMessage);
        this.showConfirmDialog = false;
        this.pendingDeleteId = null;
        console.error('Error deleting vehicle:', error);
      }
    });
  }

  onCancelDelete(): void {
    this.showConfirmDialog = false;
    this.confirmDialogLoading = false;
    this.pendingDeleteId = null;
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

  setViewMode(mode: 'table' | 'gallery'): void {
    if (this.viewMode === mode) {
      return;
    }
    this.viewMode = mode;
    localStorage.setItem('volt-vehicles-view', mode);
  }

  statusLabel(status: VehicleStatus | number): string {
    switch (status) {
      case VehicleStatus.Available:
        return this.localeService.translate('vehicles.available');
      case VehicleStatus.UnderMaintenance:
        return this.localeService.translate('vehicles.maintenance');
      case VehicleStatus.Rented:
        return this.localeService.translate('vehicles.rented');
      default:
        return String(status);
    }
  }

  statusClass(status: VehicleStatus | number): string {
    switch (status) {
      case VehicleStatus.Available:
        return 'vehicles__status--available';
      case VehicleStatus.UnderMaintenance:
        return 'vehicles__status--maintenance';
      case VehicleStatus.Rented:
        return 'vehicles__status--rented';
      default:
        return '';
    }
  }

  private readStoredViewMode(): 'table' | 'gallery' {
    const stored = localStorage.getItem('volt-vehicles-view');
    return stored === 'table' ? 'table' : 'gallery';
  }
}
