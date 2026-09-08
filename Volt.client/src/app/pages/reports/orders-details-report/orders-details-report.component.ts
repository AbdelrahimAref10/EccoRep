import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  AdminCustomerClient,
  AdminReportClient,
  CityClient,
  OrderState,
  OrdersDetailsReportDto,
  PaymentMethod,
  PaymentState,
  ReportExportFormat
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';
import {
  downloadFileResponse,
  emptyToNull,
  optionalDate,
  toNumberArray
} from '../report-utils';

@Component({
  selector: 'app-orders-details-report',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './orders-details-report.component.html',
  styleUrls: ['../report-page-shared.css']
})
export class OrdersDetailsReportComponent implements OnInit {
  private readonly locale = inject(LocaleService);
  private readonly reportClient = inject(AdminReportClient);
  private readonly cityClient = inject(CityClient);
  private readonly customerClient = inject(AdminCustomerClient);
  private readonly router = inject(Router);

  report: OrdersDetailsReportDto | null = null;
  isLoading = false;
  isExporting = false;
  errorMessage = '';
  hasSearched = false;

  cityIds: number[] = [];
  orderStates: number[] = [];
  customerIds: number[] = [];
  paymentMethodIds: number[] = [];
  paymentStates: number[] = [];
  fromDate = '';
  toDate = '';
  reservationFrom = '';
  reservationTo = '';
  orderCode = '';
  isCancelled: boolean | null = null;

  cityOptions: MultiSelectOption[] = [];
  customerOptions: MultiSelectOption[] = [];

  get stateOptions(): MultiSelectOption[] {
    return [
      { value: OrderState.Pending, label: this.locale.translate('common.pending') },
      { value: OrderState.Confirmed, label: this.locale.translate('common.confirmed') },
      { value: OrderState.OnWay, label: this.locale.translate('common.onWay') },
      { value: OrderState.CustomerReceived, label: this.locale.translate('common.received') },
      { value: OrderState.Completed, label: this.locale.translate('common.completed') },
      { value: OrderState.Cancelled, label: this.locale.translate('orders.cancelled') }
    ];
  }

  get paymentMethodOptions(): MultiSelectOption[] {
    return [
      { value: PaymentMethod.Cash, label: this.locale.translate('common.cash') },
      { value: PaymentMethod.PayPal, label: this.locale.translate('common.paypal') }
    ];
  }

  get paymentStateOptions(): MultiSelectOption[] {
    return [
      { value: PaymentState.Pending, label: this.locale.translate('common.pending') },
      { value: PaymentState.Paid, label: this.locale.translate('common.paid') },
      { value: PaymentState.Failed, label: this.locale.translate('common.failed') },
      { value: PaymentState.Refunded, label: this.locale.translate('common.refunded') }
    ];
  }

  get cancelledOptions(): MultiSelectOption[] {
    return [
      { value: true, label: this.locale.translate('reports.yes') },
      { value: false, label: this.locale.translate('reports.no') }
    ];
  }

  ngOnInit(): void {
    this.loadLookups();
    this.search();
  }

  back(): void {
    this.router.navigate(['/main/reports']);
  }

  onCitiesChange(values: Array<string | number | boolean>): void {
    this.cityIds = toNumberArray(values);
  }

  onStatesChange(values: Array<string | number | boolean>): void {
    this.orderStates = toNumberArray(values);
  }

  onCustomersChange(values: Array<string | number | boolean>): void {
    this.customerIds = toNumberArray(values);
  }

  onPaymentMethodsChange(values: Array<string | number | boolean>): void {
    this.paymentMethodIds = toNumberArray(values);
  }

  onPaymentStatesChange(values: Array<string | number | boolean>): void {
    this.paymentStates = toNumberArray(values);
  }

  onCancelledChange(values: Array<string | number | boolean>): void {
    this.isCancelled = values.length ? Boolean(values[0]) : null;
  }

  clearFilters(): void {
    this.cityIds = [];
    this.orderStates = [];
    this.customerIds = [];
    this.paymentMethodIds = [];
    this.paymentStates = [];
    this.fromDate = '';
    this.toDate = '';
    this.reservationFrom = '';
    this.reservationTo = '';
    this.orderCode = '';
    this.isCancelled = null;
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.hasSearched = true;

    this.reportClient.getOrdersDetails(
      emptyToNull(this.cityIds),
      emptyToNull(this.orderStates),
      emptyToNull(this.customerIds),
      emptyToNull(this.paymentMethodIds),
      emptyToNull(this.paymentStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      optionalDate(this.reservationFrom),
      optionalDate(this.reservationTo),
      this.orderCode.trim() || null,
      this.isCancelled
    ).subscribe({
      next: (report) => {
        this.report = report;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err?.result?.errorMessage || err?.message || this.locale.translate('reports.loadFailed');
        this.isLoading = false;
      }
    });
  }

  export(format: ReportExportFormat): void {
    this.isExporting = true;
    this.reportClient.exportOrdersDetails(
      emptyToNull(this.cityIds),
      emptyToNull(this.orderStates),
      emptyToNull(this.customerIds),
      emptyToNull(this.paymentMethodIds),
      emptyToNull(this.paymentStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      optionalDate(this.reservationFrom),
      optionalDate(this.reservationTo),
      this.orderCode.trim() || null,
      this.isCancelled,
      format
    ).subscribe({
      next: (file) => {
        downloadFileResponse(file, `OrdersDetails.${format === ReportExportFormat.Pdf ? 'pdf' : 'xlsx'}`);
        this.isExporting = false;
      },
      error: (err) => {
        this.errorMessage = err?.result?.errorMessage || err?.message || this.locale.translate('reports.exportFailed');
        this.isExporting = false;
      }
    });
  }

  formatMoney(value: number | null | undefined): string {
    return `${(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  stateLabel(state: OrderState): string {
    switch (state) {
      case OrderState.Pending: return this.locale.translate('common.pending');
      case OrderState.Confirmed: return this.locale.translate('common.confirmed');
      case OrderState.OnWay: return this.locale.translate('common.onWay');
      case OrderState.CustomerReceived: return this.locale.translate('common.received');
      case OrderState.Completed: return this.locale.translate('common.completed');
      case OrderState.Cancelled: return this.locale.translate('orders.cancelled');
      default: return '-';
    }
  }

  paymentMethodLabel(method: PaymentMethod): string {
    return method === PaymentMethod.PayPal
      ? this.locale.translate('common.paypal')
      : this.locale.translate('common.cash');
  }

  paymentStateLabel(state: PaymentState | null | undefined): string {
    if (state == null) return '-';
    switch (state) {
      case PaymentState.Pending: return this.locale.translate('common.pending');
      case PaymentState.Paid: return this.locale.translate('common.paid');
      case PaymentState.Failed: return this.locale.translate('common.failed');
      case PaymentState.Refunded: return this.locale.translate('common.refunded');
      default: return '-';
    }
  }

  paymentStateClass(state: PaymentState | null | undefined): string {
    switch (state) {
      case PaymentState.Paid: return 'rp__chip--ok';
      case PaymentState.Pending: return 'rp__chip--warn';
      case PaymentState.Failed: return 'rp__chip--bad';
      case PaymentState.Refunded: return 'rp__chip--info';
      default: return 'rp__chip--muted';
    }
  }

  readonly ReportExportFormat = ReportExportFormat;

  private loadLookups(): void {
    this.cityClient.getAll(1, 500).subscribe({
      next: (res) => {
        this.cityOptions = (res.items || []).map(c => ({ value: c.cityId, label: c.name }));
      }
    });

    this.customerClient.getAll(1, 500).subscribe({
      next: (res) => {
        this.customerOptions = (res.items || []).map(c => ({
          value: c.customerId,
          label: `${c.fullName} (${c.mobileNumber})`
        }));
      }
    });
  }
}
