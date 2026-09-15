import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CityClient, CityDto, PagedResultOfCityDto } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-cities',
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
  templateUrl: './cities.component.html',
  styleUrls: ['./cities.component.css', '../../shared/styles/list-filters.css']
})
export class CitiesComponent implements OnInit {
  private readonly localeService = inject(LocaleService);

  cities: CityDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  searchTerm = '';
  selectedStatusValues: Array<string | number | boolean> = ['true'];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  showConfirmDialog = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogType: 'danger' | 'warning' | 'info' = 'danger';
  confirmDialogLoading = false;
  pendingAction: 'deactivate' | 'activate' | 'permanentDelete' | null = null;
  pendingCityId: number | null = null;

  constructor(
    private cityClient: CityClient,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCities();
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
    count += this.selectedStatusValues.length;
    return count;
  }

  loadCities(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const isActive = this.toNullableBool(this.selectedStatusValues);
    this.cityClient.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      isActive
    ).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading cities:', error);
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadCities();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatusValues = [];
    this.applyFilters();
  }

  onStatusChange(values: Array<string | number | boolean>): void {
    this.selectedStatusValues = values;
  }

  onAddNew(): void {
    this.router.navigate(['/main/cities/new']);
  }

  onEdit(city: CityDto): void {
    this.router.navigate(['/main/cities', city.cityId, 'edit']);
  }

  onZoneRates(city: CityDto): void {
    this.router.navigate(['/main/cities', city.cityId, 'rates']);
  }

  onDelete(cityId: number): void {
    this.pendingCityId = cityId;
    this.pendingAction = 'deactivate';
    this.confirmDialogTitle = this.localeService.translate('cities.deleteTitle');
    this.confirmDialogMessage = this.localeService.translate('cities.deleteMessage');
    this.confirmDialogType = 'warning';
    this.showConfirmDialog = true;
  }

  onActivate(cityId: number): void {
    this.pendingCityId = cityId;
    this.pendingAction = 'activate';
    this.confirmDialogTitle = this.localeService.translate('cities.edit');
    this.confirmDialogMessage = this.localeService.translate('common.areYouSure');
    this.confirmDialogType = 'info';
    this.showConfirmDialog = true;
  }

  onPermanentlyDelete(cityId: number): void {
    this.pendingCityId = cityId;
    this.pendingAction = 'permanentDelete';
    this.confirmDialogTitle = this.localeService.translate('cities.deleteTitle');
    this.confirmDialogMessage = this.localeService.translate('cities.deleteMessage');
    this.confirmDialogType = 'danger';
    this.showConfirmDialog = true;
  }

  onConfirmAction(): void {
    if (this.pendingCityId === null || this.pendingAction === null) return;

    this.confirmDialogLoading = true;
    const action = this.pendingAction;
    const cityId = this.pendingCityId;
    let successMessage = '';

    switch (action) {
      case 'deactivate':
        successMessage = this.localeService.translate('common.updatedSuccessfully');
        this.cityClient.deactivate(cityId).subscribe({
          next: () => {
            this.showConfirmDialog = false;
            this.confirmDialogLoading = false;
            this.pendingCityId = null;
            this.pendingAction = null;
            this.showSuccessMessage(successMessage);
            this.loadCities();
          },
          error: (error: any) => {
            this.confirmDialogLoading = false;
            const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('common.failedToSave');
            this.showErrorMessage(errorMessage);
            this.showConfirmDialog = false;
            this.pendingCityId = null;
            this.pendingAction = null;
            console.error('Error deactivating city:', error);
          }
        });
        break;
      case 'activate':
        successMessage = this.localeService.translate('common.updatedSuccessfully');
        this.cityClient.activate(cityId).subscribe({
          next: () => {
            this.showConfirmDialog = false;
            this.confirmDialogLoading = false;
            this.pendingCityId = null;
            this.pendingAction = null;
            this.showSuccessMessage(successMessage);
            this.loadCities();
          },
          error: (error: any) => {
            this.confirmDialogLoading = false;
            const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('cities.failedToActivate');
            this.showErrorMessage(errorMessage);
            this.showConfirmDialog = false;
            this.pendingCityId = null;
            this.pendingAction = null;
            console.error('Error activating city:', error);
          }
        });
        break;
      case 'permanentDelete':
        successMessage = this.localeService.translate('common.deletedSuccessfully');
        this.cityClient.permanentlyDelete(cityId).subscribe({
          next: () => {
            this.showConfirmDialog = false;
            this.confirmDialogLoading = false;
            this.pendingCityId = null;
            this.pendingAction = null;
            this.showSuccessMessage(successMessage);
            this.loadCities();
          },
          error: (error: any) => {
            this.confirmDialogLoading = false;
            const errorMessage = this.extractErrorMessage(error) || this.localeService.translate('cities.failedToDelete');
            this.showErrorMessage(errorMessage);
            this.showConfirmDialog = false;
            this.pendingCityId = null;
            this.pendingAction = null;
            console.error('Error permanently deleting city:', error);
          }
        });
        break;
      default:
        this.confirmDialogLoading = false;
        return;
    }
  }

  onCancelAction(): void {
    this.showConfirmDialog = false;
    this.confirmDialogLoading = false;
    this.pendingCityId = null;
    this.pendingAction = null;
  }

  extractErrorMessage(error: any): string {
    if (error.error) {
      if (error.error.errorMessage) {
        return error.error.errorMessage;
      } else if (error.error.detail) {
        return error.error.detail;
      } else if (error.error.title) {
        return error.error.title;
      } else if (typeof error.error === 'string') {
        return error.error;
      }
    } else if (error.errorMessage) {
      return error.errorMessage;
    } else if (error.message) {
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

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadCities();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  private toNullableBool(values: Array<string | number | boolean>): boolean | undefined {
    if (values.length !== 1) {
      return undefined;
    }
    return String(values[0]) === 'true';
  }
}
