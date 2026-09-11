import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  CategoryLookupDto,
  MerchantClient,
  SubCategoryDto,
  VehicleDto
} from '../../../core/services/clientAPI';
import { VehicleStatus } from '../../../core/enums/vehicle-status.enum';
import { LocaleService } from '../../../core/services/locale.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-vehicles',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ConfirmDialogComponent,
    PaginationComponent,
    TranslatePipe
  ],
  templateUrl: './merchant-vehicles.component.html',
  styleUrls: [
    './merchant-vehicles.component.css',
    '../../../shared/styles/list-filters.css',
    '../../vehicles/vehicles.component.css',
    '../../../shared/styles/entity-tiles.css'
  ]
})
export class MerchantVehiclesComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);
  private readonly router = inject(Router);

  vehicles: VehicleDto[] = [];
  categories: CategoryLookupDto[] = [];
  subCategories: SubCategoryDto[] = [];

  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  totalPages = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  viewMode: 'table' | 'gallery' = this.readStoredViewMode();

  searchTerm = '';
  selectedCategoryId: number | null = null;
  selectedSubCategoryId: number | null = null;
  selectedStatus: number | null = null;

  showConfirmDialog = false;
  confirmDialogLoading = false;
  pendingDeleteId: number | null = null;

  readonly statusOptions = [
    { value: VehicleStatus.Available, key: 'vehicles.available' },
    { value: VehicleStatus.UnderMaintenance, key: 'vehicles.maintenance' },
    { value: VehicleStatus.Rented, key: 'vehicles.rented' }
  ];

  ngOnInit(): void {
    this.loadCategories();
    this.loadVehicles();
  }

  loadCategories(): void {
    this.merchantClient.getCategories().subscribe({
      next: (list) => (this.categories = list || []),
      error: () => (this.categories = [])
    });
  }

  onCategoryChange(): void {
    this.selectedSubCategoryId = null;
    this.subCategories = [];
    if (!this.selectedCategoryId) {
      return;
    }
    this.merchantClient.getSubCategoriesByCategory(this.selectedCategoryId).subscribe({
      next: (list) => (this.subCategories = list || []),
      error: () => (this.subCategories = [])
    });
  }

  loadVehicles(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.merchantClient
      .getMyVehicles(
        this.currentPage,
        this.pageSize,
        this.searchTerm.trim() || undefined,
        this.selectedStatus ?? undefined,
        this.selectedCategoryId ?? undefined,
        this.selectedSubCategoryId ?? undefined
      )
      .subscribe({
        next: (result) => {
          this.vehicles = result.items || [];
          this.totalCount = result.totalCount || 0;
          this.totalPages = result.totalPages || 0;
          this.isLoading = false;
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('merchant.vehiclesLoadFailed');
        }
      });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadVehicles();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategoryId = null;
    this.selectedSubCategoryId = null;
    this.selectedStatus = null;
    this.subCategories = [];
    this.applyFilters();
  }

  selectStatus(status: number | null): void {
    this.selectedStatus = status;
    this.applyFilters();
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadVehicles();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  onAddNew(): void {
    this.router.navigate(['/merchant/vehicles/new']);
  }

  onEdit(vehicle: VehicleDto): void {
    this.router.navigate(['/merchant/vehicles', vehicle.vehicleId, 'edit']);
  }

  onDelete(id: number): void {
    this.pendingDeleteId = id;
    this.showConfirmDialog = true;
  }

  onConfirmDelete(): void {
    if (!this.pendingDeleteId) return;
    this.confirmDialogLoading = true;
    this.merchantClient.deleteVehicle(this.pendingDeleteId).subscribe({
      next: () => {
        this.showConfirmDialog = false;
        this.confirmDialogLoading = false;
        this.pendingDeleteId = null;
        this.successMessage = this.localeService.translate('merchant.vehicleDeleted');
        setTimeout(() => (this.successMessage = ''), 5000);
        this.loadVehicles();
      },
      error: (error: any) => {
        this.confirmDialogLoading = false;
        this.showConfirmDialog = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.vehicleDeleteFailed');
      }
    });
  }

  onCancelDelete(): void {
    this.showConfirmDialog = false;
    this.pendingDeleteId = null;
  }

  setViewMode(mode: 'table' | 'gallery'): void {
    if (this.viewMode === mode) return;
    this.viewMode = mode;
    localStorage.setItem('volt-merchant-vehicles-view', mode);
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
    const stored = localStorage.getItem('volt-merchant-vehicles-view');
    return stored === 'table' ? 'table' : 'gallery';
  }
}
