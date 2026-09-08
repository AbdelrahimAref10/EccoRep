import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  AdminCustomerClient,
  AdminReportClient,
  CancelledOrdersReportDto,
  CityClient,
  CustomerWalletState,
  PaymentMethod,
  ReportExportFormat
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { MultiSelectComponent, MultiSelectOption } from '../../../shared/components/multi-select/multi-select.component';
import { downloadFileResponse, emptyToNull, optionalDate, toNumberArray } from '../report-utils';

@Component({
  selector: 'app-cancelled-orders-report',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './cancelled-orders-report.component.html',
  styleUrls: ['../report-page-shared.css']
})
export class CancelledOrdersReportComponent implements OnInit {
  private readonly locale = inject(LocaleService);
  private readonly reportClient = inject(AdminReportClient);
  private readonly cityClient = inject(CityClient);
  private readonly customerClient = inject(AdminCustomerClient);
  private readonly router = inject(Router);

  report: CancelledOrdersReportDto | null = null;
  isLoading = false;
  isExporting = false;
  errorMessage = '';

  cityIds: number[] = [];
  customerIds: number[] = [];
  paymentMethodIds: number[] = [];
  cancellationFeeStates: number[] = [];
  fromDate = '';
  toDate = '';
  orderCode = '';
  cancellationFeePaid: boolean | null = null;
  moneyRefunded: boolean | null = null;

  cityOptions: MultiSelectOption[] = [];
  customerOptions: MultiSelectOption[] = [];
  readonly ReportExportFormat = ReportExportFormat;

  get paymentMethodOptions(): MultiSelectOption[] {
    return [
      { value: PaymentMethod.Cash, label: this.locale.translate('common.cash') },
      { value: PaymentMethod.PayPal, label: this.locale.translate('common.paypal') }
    ];
  }

  get feeStateOptions(): MultiSelectOption[] {
    return [
      { value: CustomerWalletState.Pending, label: this.locale.translate('common.pending') },
      { value: CustomerWalletState.Paid, label: this.locale.translate('common.paid') },
      { value: CustomerWalletState.UnderPayment, label: this.locale.translate('reports.underPayment') }
    ];
  }

  get boolOptions(): MultiSelectOption[] {
    return [
      { value: true, label: this.locale.translate('reports.yes') },
      { value: false, label: this.locale.translate('reports.no') }
    ];
  }

  ngOnInit(): void {
    this.cityClient.getAll(1, 500).subscribe({
      next: (res) => this.cityOptions = (res.items || []).map(c => ({ value: c.cityId, label: c.name }))
    });
    this.customerClient.getAll(1, 500).subscribe({
      next: (res) => this.customerOptions = (res.items || []).map(c => ({
        value: c.customerId,
        label: `${c.fullName} (${c.mobileNumber})`
      }))
    });
    this.search();
  }

  back(): void { this.router.navigate(['/main/reports']); }

  onCitiesChange(v: Array<string | number | boolean>): void { this.cityIds = toNumberArray(v); }
  onCustomersChange(v: Array<string | number | boolean>): void { this.customerIds = toNumberArray(v); }
  onPaymentMethodsChange(v: Array<string | number | boolean>): void { this.paymentMethodIds = toNumberArray(v); }
  onFeeStatesChange(v: Array<string | number | boolean>): void { this.cancellationFeeStates = toNumberArray(v); }
  onFeePaidChange(v: Array<string | number | boolean>): void { this.cancellationFeePaid = v.length ? Boolean(v[0]) : null; }
  onMoneyRefundedChange(v: Array<string | number | boolean>): void { this.moneyRefunded = v.length ? Boolean(v[0]) : null; }

  clearFilters(): void {
    this.cityIds = [];
    this.customerIds = [];
    this.paymentMethodIds = [];
    this.cancellationFeeStates = [];
    this.fromDate = '';
    this.toDate = '';
    this.orderCode = '';
    this.cancellationFeePaid = null;
    this.moneyRefunded = null;
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.reportClient.getCancelledOrders(
      emptyToNull(this.cityIds),
      emptyToNull(this.customerIds),
      emptyToNull(this.paymentMethodIds),
      emptyToNull(this.cancellationFeeStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.orderCode.trim() || null,
      this.cancellationFeePaid,
      this.moneyRefunded
    ).subscribe({
      next: (report) => { this.report = report; this.isLoading = false; },
      error: (err) => {
        this.errorMessage = err?.result?.errorMessage || err?.message || this.locale.translate('reports.loadFailed');
        this.isLoading = false;
      }
    });
  }

  export(format: ReportExportFormat): void {
    this.isExporting = true;
    this.reportClient.exportCancelledOrders(
      emptyToNull(this.cityIds),
      emptyToNull(this.customerIds),
      emptyToNull(this.paymentMethodIds),
      emptyToNull(this.cancellationFeeStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.orderCode.trim() || null,
      this.cancellationFeePaid,
      this.moneyRefunded,
      format
    ).subscribe({
      next: (file) => {
        downloadFileResponse(file, `CancelledOrders.${format === ReportExportFormat.Pdf ? 'pdf' : 'xlsx'}`);
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

  paymentMethodLabel(method: PaymentMethod): string {
    return method === PaymentMethod.PayPal
      ? this.locale.translate('common.paypal')
      : this.locale.translate('common.cash');
  }

  feeStateLabel(state: CustomerWalletState | null | undefined): string {
    if (state == null) return '-';
    if (state === CustomerWalletState.Paid) return this.locale.translate('common.paid');
    if (state === CustomerWalletState.UnderPayment) return this.locale.translate('reports.underPayment');
    return this.locale.translate('common.pending');
  }
}
