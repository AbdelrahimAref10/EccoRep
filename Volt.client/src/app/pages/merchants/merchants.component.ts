import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AdminMerchantClient, MerchantDto } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-merchants',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent, ConfirmDialogComponent],
  templateUrl: './merchants.component.html',
  styleUrls: ['./merchants.component.css']
})
export class MerchantsComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(AdminMerchantClient);
  private readonly router = inject(Router);

  merchants: MerchantDto[] = [];
  searchTerm = '';
  deletedFilterValue: 'active' | 'deleted' = 'active';
  isLoading = false;
  isDeleting = false;
  pendingDeleteId: number | null = null;
  showConfirmDialog = false;
  errorMessage = '';
  successMessage = '';

  get deletedFilterOptions(): MultiSelectOption[] {
    return [
      { value: 'active', label: this.localeService.translate('merchants.filterNotDeleted') },
      { value: 'deleted', label: this.localeService.translate('merchants.filterDeleted') }
    ];
  }

  get isDeletedFilter(): boolean | null {
    return this.deletedFilterValue === 'deleted' ? true : null;
  }

  ngOnInit(): void {
    this.loadMerchants();
  }

  loadMerchants(): void {
    this.isLoading = true;
    this.errorMessage = '';
    const search = this.searchTerm.trim() || undefined;
    this.merchantClient.getAll(search, this.isDeletedFilter).subscribe({
      next: (list) => {
        this.merchants = list || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchants.loadFailed');
      }
    });
  }

  applyFilters(): void {
    this.loadMerchants();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.deletedFilterValue = 'active';
    this.loadMerchants();
  }

  onAddNew(): void {
    this.router.navigate(['/main/merchants/new']);
  }

  onEdit(merchant: MerchantDto): void {
    if (merchant.isDeleted) return;
    this.router.navigate(['/main/merchants', merchant.merchantId, 'edit']);
  }

  onDelete(merchant: MerchantDto): void {
    if (merchant.isDeleted || this.isDeleting) return;
    this.pendingDeleteId = merchant.merchantId;
    this.showConfirmDialog = true;
  }

  onCancelDelete(): void {
    this.showConfirmDialog = false;
    this.pendingDeleteId = null;
  }

  onConfirmDelete(): void {
    if (this.pendingDeleteId == null || this.isDeleting) return;

    this.isDeleting = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.merchantClient.delete(this.pendingDeleteId).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showConfirmDialog = false;
        this.pendingDeleteId = null;
        this.successMessage = this.localeService.translate('merchants.deleteSuccess');
        this.loadMerchants();
      },
      error: (error: any) => {
        this.isDeleting = false;
        this.showConfirmDialog = false;
        this.pendingDeleteId = null;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchants.deleteFailed');
      }
    });
  }

  statusLabel(isActive: boolean): string {
    return isActive
      ? this.localeService.translate('common.active')
      : this.localeService.translate('common.inactive');
  }
}
