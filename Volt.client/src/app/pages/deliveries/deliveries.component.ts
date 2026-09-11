import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AdminDeliveryClient, DeliveryDto } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-deliveries',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './deliveries.component.html',
  styleUrls: ['./deliveries.component.css']
})
export class DeliveriesComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly deliveryClient = inject(AdminDeliveryClient);
  private readonly router = inject(Router);

  deliveries: DeliveryDto[] = [];
  searchTerm = '';
  isDeletedFilter: boolean | null = null;
  isLoading = false;
  isDeleting = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadDeliveries();
  }

  loadDeliveries(): void {
    this.isLoading = true;
    this.errorMessage = '';
    const search = this.searchTerm.trim() || undefined;
    this.deliveryClient.getAll(search, this.isDeletedFilter).subscribe({
      next: (list) => {
        this.deliveries = list || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('deliveries.loadFailed');
      }
    });
  }

  applyFilters(): void {
    this.loadDeliveries();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.isDeletedFilter = null;
    this.loadDeliveries();
  }

  onAddNew(): void {
    this.router.navigate(['/main/deliveries/new']);
  }

  onEdit(delivery: DeliveryDto): void {
    if (delivery.isDeleted) return;
    this.router.navigate(['/main/deliveries', delivery.deliveryId, 'edit']);
  }

  onDelete(delivery: DeliveryDto): void {
    if (delivery.isDeleted || this.isDeleting) return;
    const ok = confirm(this.localeService.translate('deliveries.deleteConfirm'));
    if (!ok) return;

    this.isDeleting = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.deliveryClient.delete(delivery.deliveryId).subscribe({
      next: () => {
        this.isDeleting = false;
        this.successMessage = this.localeService.translate('deliveries.deleteSuccess');
        this.loadDeliveries();
      },
      error: (error: any) => {
        this.isDeleting = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('deliveries.deleteFailed');
      }
    });
  }

  statusLabel(isActive: boolean): string {
    return isActive
      ? this.localeService.translate('common.active')
      : this.localeService.translate('common.inactive');
  }
}
