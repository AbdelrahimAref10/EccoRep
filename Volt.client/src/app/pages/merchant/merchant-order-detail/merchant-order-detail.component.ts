import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, debounceTime, filter, takeUntil } from 'rxjs';
import {
  AcceptMerchantOrderCommand,
  FaultParty,
  JournalDirection,
  MarkMerchantHandoverToDeliveryCommand,
  MerchantClient,
  MerchantOrderResponseStatus,
  MerchantPortalOrderDetailDto,
  MerchantPortalVehicleDto,
  MerchantVehicleResponseStatus,
  OrderJournalEntryKind,
  OrderState,
  RejectMerchantOrderCommand
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { MerchantNotificationService } from '../../../core/services/merchant-notification.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { VehicleSpecsComponent } from '../../../shared/components/vehicle-specs/vehicle-specs.component';
import { VEHICLE_LIFECYCLE_STEPS, isStepDone } from '../../../shared/order-cycle/order-vehicle-cycle';

@Component({
  selector: 'app-merchant-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, VehicleSpecsComponent],
  templateUrl: './merchant-order-detail.component.html',
  styleUrl: './merchant-order-detail.component.css'
})
export class MerchantOrderDetailComponent implements OnInit, OnDestroy {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);
  private readonly merchantNotifications = inject(MerchantNotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();

  orderId = 0;
  order: MerchantPortalOrderDetailDto | null = null;
  isLoading = false;
  actionLoading = '';
  errorMessage = '';
  successMessage = '';

  confirmedVehicleIds: number[] = [];
  showRejectModal = false;
  rejectReason = '';
  showHandoverModal = false;
  selectedHandoverVehicleIds: number[] = [];

  readonly JournalDirection = JournalDirection;
  readonly lifecycleSteps = VEHICLE_LIFECYCLE_STEPS;

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.orderId = Number(params.get('id') || 0);
      if (this.orderId > 0) {
        this.loadOrder();
      }
    });

    this.merchantNotifications.incoming$.pipe(
      filter(n => !!n && !!this.orderId && n.orderId === this.orderId),
      debounceTime(300),
      takeUntil(this.destroy$)
    ).subscribe(() => this.loadOrder(true));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadOrder(silent = false): void {
    if (!silent) {
      this.isLoading = true;
      this.errorMessage = '';
    }
    this.merchantClient.getMyOrder(this.orderId).subscribe({
      next: (data) => {
        this.order = data;
        if (!silent && data.canAccept) {
          this.confirmedVehicleIds = (data.myVehicles || [])
            .filter(v => v.merchantResponseStatus === MerchantVehicleResponseStatus.Pending)
            .map(v => v.vehicleId);
        }
        this.isLoading = false;
      },
      error: (error: any) => {
        if (!silent) {
          this.isLoading = false;
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('merchant.orderLoadFailed');
        }
      }
    });
  }

  back(): void {
    this.router.navigate(['/merchant/orders']);
  }

  isVehicleConfirmed(vehicleId: number): boolean {
    return this.confirmedVehicleIds.includes(vehicleId);
  }

  toggleConfirmedVehicle(vehicleId: number): void {
    if (this.confirmedVehicleIds.includes(vehicleId)) {
      this.confirmedVehicleIds = this.confirmedVehicleIds.filter(id => id !== vehicleId);
    } else {
      this.confirmedVehicleIds = [...this.confirmedVehicleIds, vehicleId];
    }
  }

  get skippedVehicleCount(): number {
    const pending = (this.order?.myVehicles || []).filter(
      v => v.merchantResponseStatus === MerchantVehicleResponseStatus.Pending
    ).length;
    return Math.max(0, pending - this.confirmedVehicleIds.length);
  }

  isPendingVehicle(vehicle: MerchantPortalVehicleDto): boolean {
    return vehicle.merchantResponseStatus === MerchantVehicleResponseStatus.Pending;
  }

  isDeclinedVehicle(vehicle: MerchantPortalVehicleDto): boolean {
    return vehicle.merchantResponseStatus === MerchantVehicleResponseStatus.Declined;
  }

  isConfirmedVehicle(vehicle: MerchantPortalVehicleDto): boolean {
    return vehicle.merchantResponseStatus === MerchantVehicleResponseStatus.Confirmed;
  }

  onAccept(): void {
    if (!this.confirmedVehicleIds.length) {
      this.errorMessage = this.localeService.translate('merchant.selectVehiclesToConfirm');
      return;
    }
    this.actionLoading = 'accept';
    const command = new AcceptMerchantOrderCommand();
    command.orderId = this.orderId;
    command.confirmedVehicleIds = [...this.confirmedVehicleIds];
    this.merchantClient.acceptOrder(this.orderId, command).subscribe({
      next: () => {
        this.successMessage = this.skippedVehicleCount
          ? this.localeService.translate('merchant.acceptPartialSuccess')
          : this.localeService.translate('merchant.acceptSuccess');
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

  handoverSingle(vehicleId: number): void {
    this.selectedHandoverVehicleIds = [vehicleId];
    this.confirmHandover();
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

  showVehicleCycle(orderState: OrderState): boolean {
    return orderState === OrderState.DeliveryAssigned
      || orderState === OrderState.OnWay
      || orderState === OrderState.CustomerReceived
      || orderState === OrderState.Completed;
  }

  isCycleStepDone(vehicle: MerchantPortalVehicleDto, stepId: typeof VEHICLE_LIFECYCLE_STEPS[number]['id']): boolean {
    return isStepDone(vehicle, stepId);
  }

  getFaultPartyLabel(party: FaultParty | null | undefined): string {
    switch (party) {
      case FaultParty.Merchant:
        return this.localeService.translate('orders.faultMerchant');
      case FaultParty.Delivery:
        return this.localeService.translate('orders.faultDelivery');
      case FaultParty.Company:
        return this.localeService.translate('orders.faultCompany');
      default:
        return this.localeService.translate('common.noData');
    }
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
    if (status === MerchantOrderResponseStatus.PartiallyAccepted) {
      return this.localeService.translate('merchant.responsePartial');
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

  get reservationDays(): number {
    if (!this.order?.reservationDateFrom || !this.order?.reservationDateTo) return 0;
    const from = new Date(this.order.reservationDateFrom);
    const to = new Date(this.order.reservationDateTo);
    if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime()) || from > to) return 0;
    return Math.max(1, Math.round((to.getTime() - from.getTime()) / 86400000) + 1);
  }

  getStateClass(state: OrderState): string {
    switch (state) {
      case OrderState.Pending:
        return 'mod__status--pending';
      case OrderState.MerchantPending:
        return 'mod__status--merchant-pending';
      case OrderState.MerchantConfirmed:
        return 'mod__status--merchant-confirmed';
      case OrderState.Confirmed:
        return 'mod__status--confirmed';
      case OrderState.DeliveryAssigned:
        return 'mod__status--delivery-assigned';
      case OrderState.OnWay:
        return 'mod__status--onway';
      case OrderState.CustomerReceived:
        return 'mod__status--received';
      case OrderState.CustomerRejectedReceipt:
        return 'mod__status--rejected-receipt';
      case OrderState.Completed:
        return 'mod__status--completed';
      case OrderState.Cancelled:
        return 'mod__status--cancelled';
      default:
        return '';
    }
  }

  getResponseClass(status: MerchantOrderResponseStatus): string {
    if (status === MerchantOrderResponseStatus.Accepted) return 'mod__chip--ok';
    if (status === MerchantOrderResponseStatus.Rejected) return 'mod__chip--danger';
    if (status === MerchantOrderResponseStatus.PartiallyAccepted) return 'mod__chip--warn';
    return 'mod__chip--warn';
  }

  isBusy(action: string): boolean {
    return this.actionLoading === action;
  }
}
