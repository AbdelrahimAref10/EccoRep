import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  AdminCustomerClient,
  AdminReportClient,
  PayPalRefundsReportDto,
  RefundState,
  ReportExportFormat
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { MultiSelectComponent, MultiSelectOption } from '../../../shared/components/multi-select/multi-select.component';
import { downloadFileResponse, emptyToNull, optionalDate, toNumberArray } from '../report-utils';

@Component({
  selector: 'app-paypal-refunds-report',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './paypal-refunds-report.component.html',
  styleUrls: ['../report-page-shared.css']
})
export class PayPalRefundsReportComponent implements OnInit {
  private readonly locale = inject(LocaleService);
  private readonly reportClient = inject(AdminReportClient);
  private readonly customerClient = inject(AdminCustomerClient);
  private readonly router = inject(Router);

  report: PayPalRefundsReportDto | null = null;
  isLoading = false;
  isExporting = false;
  errorMessage = '';

  customerIds: number[] = [];
  refundStates: number[] = [];
  fromDate = '';
  toDate = '';
  orderCode = '';
  moneyRefunded: boolean | null = null;
  customerOptions: MultiSelectOption[] = [];
  readonly ReportExportFormat = ReportExportFormat;

  get refundStateOptions(): MultiSelectOption[] {
    return [
      { value: RefundState.Pending, label: this.locale.translate('common.pending') },
      { value: RefundState.Success, label: this.locale.translate('reports.refundSuccess') },
      { value: RefundState.Failed, label: this.locale.translate('common.failed') }
    ];
  }

  get boolOptions(): MultiSelectOption[] {
    return [
      { value: true, label: this.locale.translate('reports.yes') },
      { value: false, label: this.locale.translate('reports.no') }
    ];
  }

  ngOnInit(): void {
    this.customerClient.getAll(1, 500).subscribe({
      next: (res) => this.customerOptions = (res.items || []).map(c => ({
        value: c.customerId,
        label: `${c.fullName} (${c.mobileNumber})`
      }))
    });
    this.search();
  }

  back(): void { this.router.navigate(['/main/reports']); }
  onCustomersChange(v: Array<string | number | boolean>): void { this.customerIds = toNumberArray(v); }
  onRefundStatesChange(v: Array<string | number | boolean>): void { this.refundStates = toNumberArray(v); }
  onMoneyRefundedChange(v: Array<string | number | boolean>): void { this.moneyRefunded = v.length ? Boolean(v[0]) : null; }

  clearFilters(): void {
    this.customerIds = [];
    this.refundStates = [];
    this.fromDate = '';
    this.toDate = '';
    this.orderCode = '';
    this.moneyRefunded = null;
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.reportClient.getPayPalRefunds(
      emptyToNull(this.customerIds),
      emptyToNull(this.refundStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.orderCode.trim() || null,
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
    this.reportClient.exportPayPalRefunds(
      emptyToNull(this.customerIds),
      emptyToNull(this.refundStates),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.orderCode.trim() || null,
      this.moneyRefunded,
      format
    ).subscribe({
      next: (file) => {
        downloadFileResponse(file, `PayPalRefunds.${format === ReportExportFormat.Pdf ? 'pdf' : 'xlsx'}`);
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

  refundStateLabel(state: RefundState): string {
    if (state === RefundState.Success) return this.locale.translate('reports.refundSuccess');
    if (state === RefundState.Failed) return this.locale.translate('common.failed');
    return this.locale.translate('common.pending');
  }

  refundStateClass(state: RefundState): string {
    if (state === RefundState.Success) return 'rp__chip--ok';
    if (state === RefundState.Failed) return 'rp__chip--bad';
    return 'rp__chip--warn';
  }
}
