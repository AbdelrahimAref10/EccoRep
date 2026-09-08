import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  AdminCustomerClient,
  AdminReportClient,
  CityClient,
  PaymentMethod,
  PaymentState,
  PaymentsReportDto,
  ReportExportFormat
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { MultiSelectComponent, MultiSelectOption } from '../../../shared/components/multi-select/multi-select.component';
import { downloadFileResponse, emptyToNull, optionalDate, toNumberArray } from '../report-utils';

@Component({
  selector: 'app-payments-report',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './payments-report.component.html',
  styleUrls: ['../report-page-shared.css']
})
export class PaymentsReportComponent implements OnInit {
  private readonly locale = inject(LocaleService);
  private readonly reportClient = inject(AdminReportClient);
  private readonly cityClient = inject(CityClient);
  private readonly customerClient = inject(AdminCustomerClient);
  private readonly router = inject(Router);

  report: PaymentsReportDto | null = null;
  isLoading = false;
  isExporting = false;
  errorMessage = '';

  cityIds: number[] = [];
  customerIds: number[] = [];
  paymentMethodIds: number[] = [];
  paymentStates: number[] = [];
  fromDate = '';
  toDate = '';
  orderCode = '';
  cityOptions: MultiSelectOption[] = [];
  customerOptions: MultiSelectOption[] = [];
  readonly ReportExportFormat = ReportExportFormat;

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
  onPaymentStatesChange(v: Array<string | number | boolean>): void { this.paymentStates = toNumberArray(v); }

  clearFilters(): void {
    this.cityIds = [];
    this.customerIds = [];
    this.paymentMethodIds = [];
    this.paymentStates = [];
    this.fromDate = '';
    this.toDate = '';
    this.orderCode = '';
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.reportClient.getPayments(
      emptyToNull(this.cityIds),
      emptyToNull(this.customerIds),
      emptyToNull(this.paymentMethodIds),
      emptyToNull(this.paymentStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.orderCode.trim() || null
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
    this.reportClient.exportPayments(
      emptyToNull(this.cityIds),
      emptyToNull(this.customerIds),
      emptyToNull(this.paymentMethodIds),
      emptyToNull(this.paymentStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.orderCode.trim() || null,
      format
    ).subscribe({
      next: (file) => {
        downloadFileResponse(file, `Payments.${format === ReportExportFormat.Pdf ? 'pdf' : 'xlsx'}`);
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

  paymentStateLabel(state: PaymentState): string {
    switch (state) {
      case PaymentState.Paid: return this.locale.translate('common.paid');
      case PaymentState.Failed: return this.locale.translate('common.failed');
      case PaymentState.Refunded: return this.locale.translate('common.refunded');
      default: return this.locale.translate('common.pending');
    }
  }

  paymentStateClass(state: PaymentState): string {
    switch (state) {
      case PaymentState.Paid: return 'rp__chip--ok';
      case PaymentState.Failed: return 'rp__chip--bad';
      case PaymentState.Refunded: return 'rp__chip--info';
      default: return 'rp__chip--warn';
    }
  }
}
