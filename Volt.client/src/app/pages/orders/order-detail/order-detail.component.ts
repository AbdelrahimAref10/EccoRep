import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  AdminAvailableVehicleItemDto,
  AdminOrderClient,
  OrderDetailDto,
  OrderState,
  PaymentMethod,
  PaymentState,
  RefundState,
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

interface BookedCalendarDay {
  date: Date;
  day: number;
  inMonth: boolean;
  isBooked: boolean;
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

  order: OrderDetailDto | null = null;
  orderId: number = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  actionLoading: string = '';

  // Vehicle Assignment (same AvailableVehicles API as admin create order)
  showVehicleModal = false;
  fleetVehicles: AdminAvailableVehicleItemDto[] = [];
  selectedVehicleIds: number[] = [];
  isLoadingVehicles = false;

  bookedDaysVehicle: AdminAvailableVehicleItemDto | null = null;
  bookedCalendarMonth: Date = new Date();

  // State Management (reserved for future granular state modal)
  showStateModal = false;
  newState: OrderState | null = null;

  // Cancel confirmation dialog
  showCancelDialog = false;
  cancelDialogLoading = false;

  // PayPal refund confirmation dialog
  showRefundDialog = false;
  refundDialogLoading = false;

  readonly pipelineSteps: PipelineStep[] = [
    { state: OrderState.Pending, key: 'common.pending' },
    { state: OrderState.Confirmed, key: 'common.confirmed' },
    { state: OrderState.OnWay, key: 'common.onWay' },
    { state: OrderState.CustomerReceived, key: 'common.received' },
    { state: OrderState.Completed, key: 'common.completed' }
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

  get assignableVehicles(): AdminAvailableVehicleItemDto[] {
    return this.fleetVehicles.filter(v => v.isAvailable);
  }

  get unavailableAssignVehicles(): AdminAvailableVehicleItemDto[] {
    return this.fleetVehicles.filter(v => !v.isAvailable);
  }

  get weekdayLabels(): string[] {
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    const base = new Date(2024, 0, 7);
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(base);
      d.setDate(base.getDate() + i);
      return d.toLocaleDateString(locale, { weekday: 'short' });
    });
  }

  // Vehicle Assignment — same AvailableVehicles API as admin create order
  onAssignVehicles(): void {
    if (!this.order) return;

    this.isLoadingVehicles = true;
    this.selectedVehicleIds = [];
    this.fleetVehicles = [];
    this.showVehicleModal = true;
    this.errorMessage = '';

    const from = this.toCalendarDate(this.order.reservationDateFrom);
    const to = this.toCalendarDate(this.order.reservationDateTo);

    this.orderClient.getAvailableVehicles(
      this.order.subCategoryId,
      this.order.cityId,
      from,
      to
    ).subscribe({
      next: (result) => {
        this.fleetVehicles = result.vehicles || [];
        this.isLoadingVehicles = false;
      },
      error: (error: any) => {
        this.showErrorMessage(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('orders.vehiclesLoadFailed')
        );
        this.isLoadingVehicles = false;
        console.error('Error loading available vehicles:', error);
      }
    });
  }

  onConfirmOrder(): void {
    if (!this.order) return;

    this.errorMessage = '';
    this.successMessage = '';

    // Backend is source of truth; still guide the admin in English before the call
    if (!this.selectedVehicleIds.length || this.selectedVehicleIds.length !== this.order.vehiclesCount) {
      this.showErrorMessage(
        this.localeService.translate('orders.selectVehiclesError', { count: this.order.vehiclesCount })
      );
      return;
    }

    if (!confirm(this.localeService.translate('orders.confirmConfirmMessage', { count: this.selectedVehicleIds.length }))) {
      return;
    }

    this.actionLoading = 'confirm';
    const command = new UpdateOrderStateCommand();
    command.orderId = this.orderId;
    command.newState = OrderState.Confirmed;
    command.vehicleIds = [...this.selectedVehicleIds];

    this.orderClient.updateOrderState(this.orderId, command).subscribe({
      next: () => {
        this.showVehicleModal = false;
        this.closeBookedDaysCalendar();
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

  onCloseVehicleModal(): void {
    this.showVehicleModal = false;
    this.selectedVehicleIds = [];
    this.fleetVehicles = [];
    this.closeBookedDaysCalendar();
  }

  toggleVehicleSelection(vehicle: AdminAvailableVehicleItemDto): void {
    if (!vehicle.isAvailable || !this.order) return;

    const index = this.selectedVehicleIds.indexOf(vehicle.vehicleId);
    if (index > -1) {
      this.selectedVehicleIds = this.selectedVehicleIds.filter(id => id !== vehicle.vehicleId);
      return;
    }

    if (this.selectedVehicleIds.length >= this.order.vehiclesCount) {
      this.showErrorMessage(
        this.localeService.translate('orders.selectVehiclesError', { count: this.order.vehiclesCount })
      );
      return;
    }

    this.selectedVehicleIds = [...this.selectedVehicleIds, vehicle.vehicleId];
  }

  /** Calendar date without timezone shift for AvailableVehicles query. */
  private toCalendarDate(value: Date | string): Date {
    const d = value instanceof Date ? value : new Date(value);
    return new Date(d.getFullYear(), d.getMonth(), d.getDate(), 12, 0, 0);
  }


  isVehicleSelected(vehicleId: number): boolean {
    return this.selectedVehicleIds.includes(vehicleId);
  }

  unavailableReasonLabel(reason: string | null | undefined): string {
    if (reason === 'UnderMaintenance') {
      return this.localeService.translate('orders.reasonUnderMaintenance');
    }
    if (reason === 'Reserved') {
      return this.localeService.translate('orders.reasonReserved');
    }
    return this.localeService.translate('orders.reasonUnavailable');
  }

  hasBookedDays(vehicle: AdminAvailableVehicleItemDto): boolean {
    return vehicle.unavailableReason === 'Reserved'
      && Array.isArray(vehicle.conflictingDates)
      && vehicle.conflictingDates.length > 0;
  }

  openBookedDaysCalendar(vehicle: AdminAvailableVehicleItemDto, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    if (!this.hasBookedDays(vehicle)) return;
    this.bookedDaysVehicle = vehicle;
    const dates = this.getSortedBookedDates(vehicle);
    this.bookedCalendarMonth = this.startOfMonth(dates[0]);
  }

  closeBookedDaysCalendar(): void {
    this.bookedDaysVehicle = null;
  }

  get bookedDaysCount(): number {
    return this.bookedDaysVehicle ? this.getSortedBookedDates(this.bookedDaysVehicle).length : 0;
  }

  get bookedMonthTitle(): string {
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    return this.bookedCalendarMonth.toLocaleDateString(locale, { month: 'long', year: 'numeric' });
  }

  get bookedMonthsLabel(): string {
    if (!this.bookedDaysVehicle) return '';
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    return this.getBookedMonthStarts(this.bookedDaysVehicle)
      .map(month => month.toLocaleDateString(locale, { month: 'long', year: 'numeric' }))
      .join(' · ');
  }

  get bookedCalendarDays(): BookedCalendarDay[] {
    if (!this.bookedDaysVehicle) return [];
    const bookedKeys = new Set(this.getSortedBookedDates(this.bookedDaysVehicle).map(d => this.dateKey(d)));
    const year = this.bookedCalendarMonth.getFullYear();
    const month = this.bookedCalendarMonth.getMonth();
    const first = new Date(year, month, 1);
    const startPad = first.getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const cells: BookedCalendarDay[] = [];

    for (let i = 0; i < startPad; i++) {
      const d = new Date(year, month, i - startPad + 1);
      cells.push({ date: d, day: d.getDate(), inMonth: false, isBooked: false });
    }

    for (let day = 1; day <= daysInMonth; day++) {
      const d = new Date(year, month, day);
      cells.push({ date: d, day, inMonth: true, isBooked: bookedKeys.has(this.dateKey(d)) });
    }

    while (cells.length % 7 !== 0) {
      const last = cells[cells.length - 1].date;
      const d = new Date(last.getFullYear(), last.getMonth(), last.getDate() + 1);
      cells.push({ date: d, day: d.getDate(), inMonth: false, isBooked: false });
    }

    return cells;
  }

  canShiftBookedMonth(delta: number): boolean {
    return this.findAdjacentBookedMonth(delta) !== null;
  }

  shiftBookedMonth(delta: number): void {
    const next = this.findAdjacentBookedMonth(delta);
    if (!next) return;
    this.bookedCalendarMonth = next;
  }

  private findAdjacentBookedMonth(delta: number): Date | null {
    if (!this.bookedDaysVehicle) return null;
    const months = this.getBookedMonthStarts(this.bookedDaysVehicle);
    const current = this.bookedCalendarMonth.getTime();
    const index = months.findIndex(m => m.getTime() === current);
    if (index < 0) return null;
    return months[index + delta] ?? null;
  }

  private getSortedBookedDates(vehicle: AdminAvailableVehicleItemDto): Date[] {
    return (vehicle.conflictingDates || [])
      .map(d => this.stripTime(new Date(d)))
      .filter(d => !Number.isNaN(d.getTime()))
      .sort((a, b) => a.getTime() - b.getTime());
  }

  private getBookedMonthStarts(vehicle: AdminAvailableVehicleItemDto): Date[] {
    const seen = new Set<string>();
    const months: Date[] = [];
    for (const date of this.getSortedBookedDates(vehicle)) {
      const start = this.startOfMonth(date);
      const key = this.dateKey(start);
      if (seen.has(key)) continue;
      seen.add(key);
      months.push(start);
    }
    return months;
  }

  private stripTime(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 12, 0, 0);
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1, 12, 0, 0);
  }

  private dateKey(date: Date): string {
    return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
  }

  // State Management
  onUpdateState(state: OrderState): void {
    if (!this.order) return;

    // Clear any previous error messages
    this.errorMessage = '';
    this.successMessage = '';

    let confirmMessage = '';
    switch (state) {
      case OrderState.Confirmed:
        confirmMessage = this.localeService.translate('orders.confirmConfirmedMessage');
        break;
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

  // Cancellation
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

  // Navigation
  onBack(): void {
    this.router.navigate(['/main/orders']);
  }

  // Helper Methods
  getStateLabel(state: OrderState): string {
    switch (state) {
      case OrderState.Pending:
        return this.localeService.translate('common.pending');
      case OrderState.Confirmed:
        return this.localeService.translate('common.confirmed');
      case OrderState.OnWay:
        return this.localeService.translate('common.onWay');
      case OrderState.CustomerReceived:
        return this.localeService.translate('common.received');
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
      case OrderState.Confirmed:
        return 'od__status--confirmed';
      case OrderState.OnWay:
        return 'od__status--onway';
      case OrderState.CustomerReceived:
        return 'od__status--received';
      case OrderState.Completed:
        return 'od__status--completed';
      case OrderState.Cancelled:
        return 'od__status--cancelled';
      default:
        return '';
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

  canConfirm(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.Pending;
  }

  canUpdateToOnWay(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.Confirmed;
  }

  canUpdateToCustomerReceived(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.OnWay;
  }

  canComplete(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.CustomerReceived;
  }

  canCancel(): boolean {
    if (this.isCancelled) return false;
    return this.order?.orderState === OrderState.Pending || this.order?.orderState === OrderState.Confirmed;
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
    // Legacy cancelled rows (before OrderState.Cancelled)
    return !!this.order.orderCancellationFee
      || !!this.order.refundablePaypalAmount
      || this.order.moneyRefunded === true;
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

  get hasPrimaryAction(): boolean {
    return this.canConfirm()
      || this.canUpdateToOnWay()
      || this.canUpdateToCustomerReceived()
      || this.canComplete();
  }

  getStepStatus(stepState: OrderState): 'done' | 'active' | 'upcoming' {
    if (!this.order || this.isCancelled) return 'upcoming';
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

  get PaymentMethod() {
    return PaymentMethod;
  }

  get PaymentState() {
    return PaymentState;
  }
}
