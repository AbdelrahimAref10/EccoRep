import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  AdminOrderClient,
  AdminAvailableVehicleItemDto,
  AdminReplaceOrderVehicleCommand,
  AssignDeliveryToOrderCommand,
  AssignDeliveryVehicleItem,
  DeliveryClient,
  DeliveryLookupDto,
  FaultParty,
  JournalDirection,
  LedgerPartyType,
  MarkCustomerRejectedReceiptCommand,
  MarkMerchantHandoverToDeliveryCommand,
  MerchantClient,
  MerchantLookupDto,
  MerchantOrderResponseStatus,
  OrderDetailDto,
  OrderJournalEntryKind,
  OrderState,
  PaymentMethod,
  PaymentState,
  ReassignMerchantOrderCommand,
  RefundState,
  SendOrderToMerchantsCommand,
  UpdateOrderStateCommand
} from '../../../core/services/clientAPI';
import { VehicleStatus } from '../../../core/enums/vehicle-status.enum';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

interface PipelineStep {
  state: OrderState;
  key: string;
}

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, ConfirmDialogComponent],
  templateUrl: './order-detail.component.html',
  styleUrls: [
    './order-detail.component.css',
    '../../../shared/styles/entity-tiles.css'
  ]
})
export class OrderDetailComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);
  private readonly deliveryClient = inject(DeliveryClient);

  order: OrderDetailDto | null = null;
  orderId: number = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  actionLoading: string = '';

  // Send to merchants
  showMerchantsModal = false;
  activeMerchants: MerchantLookupDto[] = [];
  selectedMerchantIds: number[] = [];
  isLoadingMerchants = false;

  // Reassign merchant
  showReassignModal = false;
  reassignOldMerchantId: number | null = null;
  reassignNewMerchantId: number | null = null;
  reassignMerchants: MerchantLookupDto[] = [];
  isLoadingReassign = false;

  // Replace vehicle
  showReplaceVehicleModal = false;
  replaceOldVehicleId: number | null = null;
  replaceNewVehicleId: number | null = null;
  replaceCandidates: AdminAvailableVehicleItemDto[] = [];
  isLoadingReplaceVehicles = false;

  // Assign delivery
  showDeliveryModal = false;
  activeDeliveries: DeliveryLookupDto[] = [];
  deliveryAssignments: Record<number, number | null> = {};
  isLoadingDeliveries = false;

  // Merchant handover
  showHandoverModal = false;
  selectedHandoverVehicleIds: number[] = [];

  // Reject receipt
  showRejectReceiptModal = false;
  rejectFaultParty: FaultParty = FaultParty.Customer;
  rejectNote = '';

  // Cancel / refund dialogs
  showCancelDialog = false;
  cancelDialogLoading = false;
  showRefundDialog = false;
  refundDialogLoading = false;

  readonly pipelineSteps: PipelineStep[] = [
    { state: OrderState.Pending, key: 'common.pending' },
    { state: OrderState.MerchantPending, key: 'common.merchantPending' },
    { state: OrderState.MerchantConfirmed, key: 'common.merchantConfirmed' },
    { state: OrderState.Confirmed, key: 'common.confirmed' },
    { state: OrderState.DeliveryAssigned, key: 'common.deliveryAssigned' },
    { state: OrderState.OnWay, key: 'common.onWay' },
    { state: OrderState.CustomerReceived, key: 'common.received' },
    { state: OrderState.Completed, key: 'common.completed' }
  ];

  readonly faultPartyOptions: FaultParty[] = [
    FaultParty.Customer,
    FaultParty.Merchant,
    FaultParty.Delivery,
    FaultParty.Company
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private orderClient: AdminOrderClient
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.orderId = +params['id'];
      if (this.orderId) {
        this.loadOrder();
      }
    });
  }

  loadOrder(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.orderClient.getOrderById(this.orderId).subscribe({
      next: (order: OrderDetailDto) => {
        this.order = order;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = this.localeService.translate('orders.loadFailed');
        this.isLoading = false;
        console.error('Error loading order:', error);
      }
    });
  }

  // ── Send to merchants ──────────────────────────────────────────────
  onOpenSendToMerchants(): void {
    if (!this.order) return;
    this.showMerchantsModal = true;
    this.selectedMerchantIds = [];
    this.activeMerchants = [];
    this.isLoadingMerchants = true;
    this.errorMessage = '';

    this.merchantClient.getActive().subscribe({
      next: (list) => {
        this.activeMerchants = list || [];
        this.isLoadingMerchants = false;
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.merchantsLoadFailed')
        );
        this.isLoadingMerchants = false;
      }
    });
  }

  toggleMerchantSelection(merchantId: number): void {
    const idx = this.selectedMerchantIds.indexOf(merchantId);
    if (idx > -1) {
      this.selectedMerchantIds = this.selectedMerchantIds.filter(id => id !== merchantId);
    } else {
      this.selectedMerchantIds = [...this.selectedMerchantIds, merchantId];
    }
  }

  isMerchantSelected(merchantId: number): boolean {
    return this.selectedMerchantIds.includes(merchantId);
  }

  onConfirmSendToMerchants(): void {
    if (!this.order || !this.selectedMerchantIds.length) {
      this.showErrorMessage(this.localeService.translate('orders.selectMerchantsError'));
      return;
    }

    this.actionLoading = 'sendMerchants';
    const command = new SendOrderToMerchantsCommand();
    command.orderId = this.orderId;
    command.merchantIds = [...this.selectedMerchantIds];

    this.orderClient.sendToMerchants(this.orderId, command).subscribe({
      next: () => {
        this.showMerchantsModal = false;
        this.showSuccessMessage(this.localeService.translate('orders.sendToMerchantsSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.sendToMerchantsFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCloseMerchantsModal(): void {
    this.showMerchantsModal = false;
    this.selectedMerchantIds = [];
  }

  // ── Reassign merchant ──────────────────────────────────────────────
  onOpenReassign(oldMerchantId: number): void {
    this.reassignOldMerchantId = oldMerchantId;
    this.reassignNewMerchantId = null;
    this.showReassignModal = true;
    this.isLoadingReassign = true;
    this.reassignMerchants = [];

    this.merchantClient.getActive().subscribe({
      next: (list) => {
        this.reassignMerchants = (list || []).filter(m => m.merchantId !== oldMerchantId);
        this.isLoadingReassign = false;
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.merchantsLoadFailed')
        );
        this.isLoadingReassign = false;
      }
    });
  }

  onConfirmReassign(): void {
    if (!this.reassignOldMerchantId || !this.reassignNewMerchantId) {
      this.showErrorMessage(this.localeService.translate('orders.selectNewMerchantError'));
      return;
    }

    this.actionLoading = 'reassign';
    const command = new ReassignMerchantOrderCommand();
    command.orderId = this.orderId;
    command.oldMerchantId = this.reassignOldMerchantId;
    command.newMerchantId = this.reassignNewMerchantId;

    this.orderClient.reassignMerchant(this.orderId, command).subscribe({
      next: () => {
        this.showReassignModal = false;
        this.showSuccessMessage(this.localeService.translate('orders.reassignMerchantSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.reassignMerchantFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCloseReassignModal(): void {
    this.showReassignModal = false;
    this.reassignOldMerchantId = null;
    this.reassignNewMerchantId = null;
  }

  canReplaceVehicle(): boolean {
    if (!this.order) return false;
    const state = this.order.orderState;
    return state === OrderState.Pending
      || state === OrderState.MerchantPending
      || state === OrderState.MerchantConfirmed;
  }

  onOpenReplaceVehicle(oldVehicleId: number): void {
    if (!this.order) return;
    this.replaceOldVehicleId = oldVehicleId;
    this.replaceNewVehicleId = null;
    this.replaceCandidates = [];
    this.showReplaceVehicleModal = true;
    this.isLoadingReplaceVehicles = true;

    const currentIds = new Set((this.order.orderVehicles || []).map(v => v.vehicleId));
    this.orderClient.getAvailableVehicles(
      this.order.subCategoryId,
      this.order.cityId,
      this.order.reservationDateFrom,
      this.order.reservationDateTo,
      this.orderId
    ).subscribe({
      next: (result) => {
        this.replaceCandidates = (result?.vehicles || []).filter(
          v => v.isAvailable && !currentIds.has(v.vehicleId)
        );
        this.isLoadingReplaceVehicles = false;
      },
      error: (error: any) => {
        this.isLoadingReplaceVehicles = false;
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.replaceVehicleLoadFailed')
        );
      }
    });
  }

  onConfirmReplaceVehicle(): void {
    if (!this.replaceOldVehicleId || !this.replaceNewVehicleId) {
      this.showErrorMessage(this.localeService.translate('orders.selectNewVehicleError'));
      return;
    }

    this.actionLoading = 'replaceVehicle';
    const command = new AdminReplaceOrderVehicleCommand();
    command.orderId = this.orderId;
    command.oldVehicleId = this.replaceOldVehicleId;
    command.newVehicleId = this.replaceNewVehicleId;

    this.orderClient.replaceVehicle(this.orderId, command).subscribe({
      next: () => {
        this.showReplaceVehicleModal = false;
        this.showSuccessMessage(this.localeService.translate('orders.replaceVehicleSuccess'));
        this.actionLoading = '';
        this.loadOrder();
      },
      error: (error: any) => {
        this.actionLoading = '';
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.replaceVehicleFailed')
        );
      }
    });
  }

  onCloseReplaceVehicleModal(): void {
    this.showReplaceVehicleModal = false;
    this.replaceOldVehicleId = null;
    this.replaceNewVehicleId = null;
    this.replaceCandidates = [];
  }

  // ── Confirm (MerchantConfirmed → Confirmed, no vehicles) ───────────
  onConfirmOrder(): void {
    if (!this.order) return;
    this.errorMessage = '';
    this.successMessage = '';

    if (!confirm(this.localeService.translate('orders.confirmConfirmedMessage'))) return;

    this.actionLoading = 'confirm';
    const command = new UpdateOrderStateCommand();
    command.orderId = this.orderId;
    command.newState = OrderState.Confirmed;
    command.vehicleIds = null;

    this.orderClient.updateOrderState(this.orderId, command).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('orders.confirmedSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage
          || error?.error?.errorMessage
          || error?.result?.errorMessage
          || this.localeService.translate('orders.confirmFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  // ── Assign delivery ────────────────────────────────────────────────
  onOpenAssignDelivery(): void {
    if (!this.order) return;
    this.showDeliveryModal = true;
    this.isLoadingDeliveries = true;
    this.activeDeliveries = [];
    this.deliveryAssignments = {};
    for (const v of this.order.orderVehicles || []) {
      this.deliveryAssignments[v.vehicleId] = null;
    }

    this.deliveryClient.getActive(this.order.cityId).subscribe({
      next: (list) => {
        this.activeDeliveries = list || [];
        this.isLoadingDeliveries = false;
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.deliveriesLoadFailed')
        );
        this.isLoadingDeliveries = false;
      }
    });
  }

  setDeliveryAssignment(vehicleId: number, deliveryId: number | null): void {
    this.deliveryAssignments[vehicleId] = deliveryId;
  }

  get allDeliveriesAssigned(): boolean {
    if (!this.order?.orderVehicles?.length) return false;
    return this.order.orderVehicles.every(v => !!this.deliveryAssignments[v.vehicleId]);
  }

  onConfirmAssignDelivery(): void {
    if (!this.order || !this.allDeliveriesAssigned) {
      this.showErrorMessage(this.localeService.translate('orders.assignDeliveryAllRequired'));
      return;
    }

    this.actionLoading = 'assignDelivery';
    const command = new AssignDeliveryToOrderCommand();
    command.orderId = this.orderId;
    command.assignments = this.order.orderVehicles.map(v => {
      const item = new AssignDeliveryVehicleItem();
      item.vehicleId = v.vehicleId;
      item.deliveryId = this.deliveryAssignments[v.vehicleId]!;
      return item;
    });

    this.orderClient.assignDelivery(this.orderId, command).subscribe({
      next: () => {
        this.showDeliveryModal = false;
        this.showSuccessMessage(this.localeService.translate('orders.assignDeliverySuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.assignDeliveryFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCloseDeliveryModal(): void {
    this.showDeliveryModal = false;
  }

  // ── Merchant handover ──────────────────────────────────────────────
  get unreceivedDeliveryVehicles() {
    return (this.order?.deliveryMenOrders || []).filter(d => !d.deliveryReceivedFromMerchant);
  }

  onOpenHandover(): void {
    this.selectedHandoverVehicleIds = [];
    this.showHandoverModal = true;
  }

  toggleHandoverVehicle(vehicleId: number): void {
    const idx = this.selectedHandoverVehicleIds.indexOf(vehicleId);
    if (idx > -1) {
      this.selectedHandoverVehicleIds = this.selectedHandoverVehicleIds.filter(id => id !== vehicleId);
    } else {
      this.selectedHandoverVehicleIds = [...this.selectedHandoverVehicleIds, vehicleId];
    }
  }

  isHandoverSelected(vehicleId: number): boolean {
    return this.selectedHandoverVehicleIds.includes(vehicleId);
  }

  onConfirmHandover(): void {
    if (!this.selectedHandoverVehicleIds.length) {
      this.showErrorMessage(this.localeService.translate('orders.selectHandoverVehiclesError'));
      return;
    }

    this.actionLoading = 'handover';
    const command = new MarkMerchantHandoverToDeliveryCommand();
    command.orderId = this.orderId;
    command.vehicleIds = [...this.selectedHandoverVehicleIds];

    this.orderClient.markMerchantHandover(this.orderId, command).subscribe({
      next: () => {
        this.showHandoverModal = false;
        this.showSuccessMessage(this.localeService.translate('orders.markHandoverSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.markHandoverFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCloseHandoverModal(): void {
    this.showHandoverModal = false;
    this.selectedHandoverVehicleIds = [];
  }

  // ── Reject receipt ─────────────────────────────────────────────────
  onOpenRejectReceipt(): void {
    this.rejectFaultParty = FaultParty.Customer;
    this.rejectNote = '';
    this.showRejectReceiptModal = true;
  }

  onConfirmRejectReceipt(): void {
    this.actionLoading = 'rejectReceipt';
    const command = new MarkCustomerRejectedReceiptCommand();
    command.orderId = this.orderId;
    command.faultParty = this.rejectFaultParty;
    command.note = this.rejectNote?.trim() || null;

    this.orderClient.rejectReceipt(this.orderId, command).subscribe({
      next: () => {
        this.showRejectReceiptModal = false;
        this.showSuccessMessage(this.localeService.translate('orders.rejectReceiptSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.rejectReceiptFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCloseRejectReceiptModal(): void {
    this.showRejectReceiptModal = false;
  }

  // ── State updates ──────────────────────────────────────────────────
  onUpdateState(state: OrderState): void {
    if (!this.order) return;
    this.errorMessage = '';
    this.successMessage = '';

    let confirmMessage = '';
    switch (state) {
      case OrderState.OnWay:
        confirmMessage = this.localeService.translate('orders.confirmOnWayMessage');
        break;
      case OrderState.CustomerReceived:
        confirmMessage = this.localeService.translate('orders.confirmReceivedMessage');
        break;
      case OrderState.Completed:
        confirmMessage = this.localeService.translate('orders.confirmCompletedMessage');
        break;
      default:
        return;
    }

    if (!confirm(confirmMessage)) return;

    this.actionLoading = `state-${state}`;
    const command = new UpdateOrderStateCommand();
    command.orderId = this.orderId;
    command.newState = state;

    this.orderClient.updateOrderState(this.orderId, command).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('orders.stateUpdatedSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.stateUpdateFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  // ── Cancellation ───────────────────────────────────────────────────
  onCancelOrder(): void {
    if (!this.order) return;
    this.errorMessage = '';
    this.successMessage = '';
    this.showCancelDialog = true;
  }

  onConfirmCancelOrder(): void {
    this.cancelDialogLoading = true;
    this.actionLoading = 'cancel';
    this.orderClient.rejectOrder(this.orderId).subscribe({
      next: () => {
        this.showCancelDialog = false;
        this.cancelDialogLoading = false;
        this.showSuccessMessage(this.localeService.translate('orders.rejectedSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showCancelDialog = false;
        this.cancelDialogLoading = false;
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.rejectFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCancelCancelDialog(): void {
    this.showCancelDialog = false;
  }

  onMarkCancellationFeePaid(): void {
    if (!this.order?.orderCancellationFee) return;
    this.errorMessage = '';
    this.successMessage = '';
    this.actionLoading = 'markFeePaid';
    this.orderClient.markOrderCancellationFeePaid(this.orderId).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('orders.feePaidSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.feePaidFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  canMarkMoneyRefunded(): boolean {
    if (!this.order?.refundablePaypalAmount || this.order.moneyRefunded) return false;
    return this.order.refundablePaypalAmount.state === RefundState.Pending;
  }

  onMarkMoneyRefunded(): void {
    if (!this.canMarkMoneyRefunded()) return;
    this.errorMessage = '';
    this.successMessage = '';
    this.showRefundDialog = true;
  }

  onConfirmMarkMoneyRefunded(): void {
    this.refundDialogLoading = true;
    this.actionLoading = 'markRefunded';
    this.orderClient.markMoneyRefunded(this.orderId).subscribe({
      next: () => {
        this.showRefundDialog = false;
        this.refundDialogLoading = false;
        this.showSuccessMessage(this.localeService.translate('orders.refundMarkedSuccess'));
        this.loadOrder();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showRefundDialog = false;
        this.refundDialogLoading = false;
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.refundMarkedFailed')
        );
        this.actionLoading = '';
      }
    });
  }

  onCancelRefundDialog(): void {
    this.showRefundDialog = false;
  }

  onBack(): void {
    this.router.navigate(['/main/orders']);
  }

  onGoSettlements(): void {
    this.router.navigate(['/main/settlements']);
  }

  // ── Labels / helpers ───────────────────────────────────────────────
  getStateLabel(state: OrderState): string {
    switch (state) {
      case OrderState.Pending:
        return this.localeService.translate('common.pending');
      case OrderState.MerchantPending:
        return this.localeService.translate('common.merchantPending');
      case OrderState.MerchantConfirmed:
        return this.localeService.translate('common.merchantConfirmed');
      case OrderState.Confirmed:
        return this.localeService.translate('common.confirmed');
      case OrderState.DeliveryAssigned:
        return this.localeService.translate('common.deliveryAssigned');
      case OrderState.OnWay:
        return this.localeService.translate('common.onWay');
      case OrderState.CustomerReceived:
        return this.localeService.translate('common.received');
      case OrderState.CustomerRejectedReceipt:
        return this.localeService.translate('common.rejectedReceipt');
      case OrderState.Completed:
        return this.localeService.translate('common.completed');
      case OrderState.Cancelled:
        return this.localeService.translate('orders.cancelled');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getStateClass(state: OrderState): string {
    switch (state) {
      case OrderState.Pending:
        return 'od__status--pending';
      case OrderState.MerchantPending:
        return 'od__status--merchant-pending';
      case OrderState.MerchantConfirmed:
        return 'od__status--merchant-confirmed';
      case OrderState.Confirmed:
        return 'od__status--confirmed';
      case OrderState.DeliveryAssigned:
        return 'od__status--delivery-assigned';
      case OrderState.OnWay:
        return 'od__status--onway';
      case OrderState.CustomerReceived:
        return 'od__status--received';
      case OrderState.CustomerRejectedReceipt:
        return 'od__status--rejected-receipt';
      case OrderState.Completed:
        return 'od__status--completed';
      case OrderState.Cancelled:
        return 'od__status--cancelled';
      default:
        return '';
    }
  }

  getMerchantStatusLabel(status: MerchantOrderResponseStatus): string {
    switch (status) {
      case MerchantOrderResponseStatus.Pending:
        return this.localeService.translate('common.pending');
      case MerchantOrderResponseStatus.Accepted:
        return this.localeService.translate('orders.merchantAccepted');
      case MerchantOrderResponseStatus.Rejected:
        return this.localeService.translate('orders.merchantRejected');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getFaultPartyLabel(party: FaultParty | null | undefined): string {
    switch (party) {
      case FaultParty.Customer:
        return this.localeService.translate('orders.faultCustomer');
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

  getJournalDirectionLabel(direction: JournalDirection): string {
    return direction === JournalDirection.Debit
      ? this.localeService.translate('orders.journalDebit')
      : this.localeService.translate('orders.journalCredit');
  }

  getJournalKindLabel(kind: OrderJournalEntryKind): string {
    const key = `orders.journalKind.${OrderJournalEntryKind[kind]}`;
    const translated = this.localeService.translate(key);
    return translated === key ? String(kind) : translated;
  }

  getPartyTypeLabel(partyType: LedgerPartyType): string {
    switch (partyType) {
      case LedgerPartyType.Company:
        return this.localeService.translate('orders.partyCompany');
      case LedgerPartyType.Merchant:
        return this.localeService.translate('orders.partyMerchant');
      case LedgerPartyType.Delivery:
        return this.localeService.translate('orders.partyDelivery');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getPaymentMethodLabel(method: PaymentMethod): string {
    switch (method) {
      case PaymentMethod.Cash:
        return this.localeService.translate('common.cash');
      case PaymentMethod.PayPal:
        return this.localeService.translate('common.paypal');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getPaymentStateLabel(state: PaymentState): string {
    switch (state) {
      case PaymentState.Pending:
        return this.localeService.translate('common.pending');
      case PaymentState.Paid:
        return this.localeService.translate('common.paid');
      case PaymentState.Failed:
        return this.localeService.translate('common.failed');
      case PaymentState.Refunded:
        return this.localeService.translate('common.refunded');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getPaymentStateClass(state: PaymentState): string {
    switch (state) {
      case PaymentState.Pending:
        return 'od__pay--pending';
      case PaymentState.Paid:
        return 'od__pay--paid';
      case PaymentState.Failed:
        return 'od__pay--failed';
      case PaymentState.Refunded:
        return 'od__pay--refunded';
      default:
        return '';
    }
  }

  canSendToMerchants(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.Pending;
  }

  canConfirm(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.MerchantConfirmed;
  }

  canAssignDelivery(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.Confirmed;
  }

  canUpdateToOnWay(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.DeliveryAssigned;
  }

  canMarkHandover(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.DeliveryAssigned
      && this.unreceivedDeliveryVehicles.length > 0;
  }

  canUpdateToCustomerReceived(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.OnWay;
  }

  canRejectReceipt(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.OnWay;
  }

  canComplete(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.CustomerReceived;
  }

  canCancel(): boolean {
    if (this.isCancelled) return false;
    const state = this.order?.orderState;
    return state === OrderState.Pending
      || state === OrderState.MerchantPending
      || state === OrderState.MerchantConfirmed;
  }

  canEdit(): boolean {
    if (!this.order || this.isCancelled) return false;
    if (this.order.orderState !== OrderState.Pending) return false;
    const blocked = this.order.orderPayments?.some(
      p => p.state === PaymentState.Paid || p.state === PaymentState.Refunded
    );
    return !blocked;
  }

  onEdit(): void {
    if (!this.orderId) return;
    this.router.navigate(['/main/orders', this.orderId, 'edit']);
  }

  get isCancelled(): boolean {
    if (!this.order) return false;
    if (this.order.orderState === OrderState.Cancelled) return true;
    return !!this.order.orderCancellationFee
      || !!this.order.refundablePaypalAmount
      || this.order.moneyRefunded === true;
  }

  get isRejectedReceipt(): boolean {
    return this.order?.orderState === OrderState.CustomerRejectedReceipt;
  }

  get showPipeline(): boolean {
    return !this.isCancelled && !this.isRejectedReceipt;
  }

  get displayState(): OrderState {
    if (!this.order) return OrderState.Pending;
    return this.isCancelled ? OrderState.Cancelled : this.order.orderState;
  }

  get reservationDays(): number {
    if (!this.order?.reservationDateFrom || !this.order?.reservationDateTo) return 0;
    const from = new Date(this.order.reservationDateFrom);
    const to = new Date(this.order.reservationDateTo);
    if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime()) || from > to) return 0;
    return Math.max(1, Math.round((to.getTime() - from.getTime()) / 86400000) + 1);
  }

  getVehicleStatusLabel(status: VehicleStatus | number): string {
    switch (status) {
      case VehicleStatus.Available:
        return this.localeService.translate('vehicles.available');
      case VehicleStatus.UnderMaintenance:
        return this.localeService.translate('vehicles.maintenance');
      case VehicleStatus.Rented:
        return this.localeService.translate('vehicles.rented');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getVehicleStatusBadgeClass(status: VehicleStatus | number): string {
    switch (status) {
      case VehicleStatus.Available:
        return 'od__vehicle-badge--ok';
      case VehicleStatus.UnderMaintenance:
        return 'od__vehicle-badge--warn';
      case VehicleStatus.Rented:
        return 'od__vehicle-badge--rented';
      default:
        return 'od__vehicle-badge--muted';
    }
  }

  get hasPrimaryAction(): boolean {
    return this.canSendToMerchants()
      || this.canConfirm()
      || this.canAssignDelivery()
      || this.canUpdateToOnWay()
      || this.canMarkHandover()
      || this.canUpdateToCustomerReceived()
      || this.canRejectReceipt()
      || this.canComplete()
      || this.order?.orderState === OrderState.MerchantPending;
  }

  getStepStatus(stepState: OrderState): 'done' | 'active' | 'upcoming' {
    if (!this.order || this.isCancelled || this.isRejectedReceipt) return 'upcoming';
    if (this.order.orderState > stepState) return 'done';
    if (this.order.orderState === stepState) return 'active';
    return 'upcoming';
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

  isActionLoading(action: string): boolean {
    return this.actionLoading === action;
  }

  get OrderState() {
    return OrderState;
  }

  get MerchantOrderResponseStatus() {
    return MerchantOrderResponseStatus;
  }

  get PaymentMethod() {
    return PaymentMethod;
  }

  get PaymentState() {
    return PaymentState;
  }

  get FaultParty() {
    return FaultParty;
  }
}
