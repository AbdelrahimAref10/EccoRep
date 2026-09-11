import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  JournalDirection,
  MarkMerchantHandoverToDeliveryCommand,
  MerchantClient,
  MerchantOrderResponseStatus,
  MerchantPortalOrderDetailDto,
  OrderJournalEntryKind,
  OrderState,
  RejectMerchantOrderCommand
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './merchant-order-detail.component.html',
  styleUrl: './merchant-order-detail.component.css'
})
export class MerchantOrderDetailComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  orderId = 0;
  order: MerchantPortalOrderDetailDto | null = null;
  isLoading = false;
  actionLoading = '';
  errorMessage = '';
  successMessage = '';

  showRejectModal = false;
  rejectReason = '';
  showHandoverModal = false;
  selectedHandoverVehicleIds: number[] = [];

  readonly JournalDirection = JournalDirection;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.orderId = Number(params.get('id') || 0);
      if (this.orderId > 0) {
        this.loadOrder();
      }
    });
  }

  loadOrder(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.merchantClient.getMyOrder(this.orderId).subscribe({
      next: (data) => {
        this.order = data;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.orderLoadFailed');
      }
    });
  }

  back(): void {
    this.router.navigate(['/merchant/orders']);
  }

  onAccept(): void {
    this.actionLoading = 'accept';
    this.merchantClient.acceptOrder(this.orderId).subscribe({
      next: () => {
        this.successMessage = this.localeService.translate('merchant.acceptSuccess');
        this.actionLoading = '';
        this.loadOrder();
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.acceptFailed');
        this.actionLoading = '';
      }
    });
  }

  openReject(): void {
    this.rejectReason = '';
    this.showRejectModal = true;
  }

  confirmReject(): void {
    if (!this.rejectReason.trim()) {
      this.errorMessage = this.localeService.translate('merchant.rejectReasonRequired');
      return;
    }
    this.actionLoading = 'reject';
    const command = new RejectMerchantOrderCommand();
    command.orderId = this.orderId;
    command.reason = this.rejectReason.trim();
    this.merchantClient.rejectOrder(this.orderId, command).subscribe({
      next: () => {
        this.showRejectModal = false;
        this.successMessage = this.localeService.translate('merchant.rejectSuccess');
        this.actionLoading = '';
        this.loadOrder();
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.rejectFailed');
        this.actionLoading = '';
      }
    });
  }

  get pendingHandoverVehicles() {
    return (this.order?.myHandovers || []).filter(
      h => h.assignedToDelivery && !h.deliveryReceivedFromMerchant
    );
  }

  openHandover(): void {
    this.selectedHandoverVehicleIds = [];
    this.showHandoverModal = true;
  }

  toggleHandoverVehicle(vehicleId: number): void {
    if (this.selectedHandoverVehicleIds.includes(vehicleId)) {
      this.selectedHandoverVehicleIds = this.selectedHandoverVehicleIds.filter(id => id !== vehicleId);
    } else {
      this.selectedHandoverVehicleIds = [...this.selectedHandoverVehicleIds, vehicleId];
    }
  }

  confirmHandover(): void {
    if (!this.selectedHandoverVehicleIds.length) {
      this.errorMessage = this.localeService.translate('orders.selectHandoverVehiclesError');
      return;
    }
    this.actionLoading = 'handover';
    const command = new MarkMerchantHandoverToDeliveryCommand();
    command.orderId = this.orderId;
    command.vehicleIds = [...this.selectedHandoverVehicleIds];
    this.merchantClient.handoverToDelivery(this.orderId, command).subscribe({
      next: () => {
        this.showHandoverModal = false;
        this.successMessage = this.localeService.translate('orders.markHandoverSuccess');
        this.actionLoading = '';
        this.loadOrder();
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('orders.markHandoverFailed');
        this.actionLoading = '';
      }
    });
  }

  getStateLabel(state: OrderState): string {
    const map: Record<number, string> = {
      [OrderState.Pending]: 'common.pending',
      [OrderState.MerchantPending]: 'common.merchantPending',
      [OrderState.MerchantConfirmed]: 'common.merchantConfirmed',
      [OrderState.Confirmed]: 'common.confirmed',
      [OrderState.DeliveryAssigned]: 'common.deliveryAssigned',
      [OrderState.OnWay]: 'common.onWay',
      [OrderState.CustomerReceived]: 'common.received',
      [OrderState.CustomerRejectedReceipt]: 'common.rejectedReceipt',
      [OrderState.Completed]: 'common.completed',
      [OrderState.Cancelled]: 'orders.cancelled'
    };
    return this.localeService.translate(map[state] || 'common.pending');
  }

  getResponseLabel(status: MerchantOrderResponseStatus): string {
    if (status === MerchantOrderResponseStatus.Accepted) {
      return this.localeService.translate('merchant.responseAccepted');
    }
    if (status === MerchantOrderResponseStatus.Rejected) {
      return this.localeService.translate('merchant.responseRejected');
    }
    return this.localeService.translate('merchant.responsePending');
  }

  getJournalKindLabel(kind: OrderJournalEntryKind): string {
    const key = `orders.journalKind.${OrderJournalEntryKind[kind]}`;
    const translated = this.localeService.translate(key);
    return translated !== key ? translated : String(kind);
  }

  formatMoney(value: number | undefined): string {
    return `${Number(value || 0).toFixed(2)} ${this.localeService.translate('common.currency')}`;
  }

  isBusy(action: string): boolean {
    return this.actionLoading === action;
  }
}
