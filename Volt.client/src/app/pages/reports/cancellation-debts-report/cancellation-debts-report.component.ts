import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  AdminCustomerClient,
  AdminReportClient,
  CancellationDebtsReportDto,
  CustomerWalletState,
  ReportExportFormat
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { MultiSelectComponent, MultiSelectOption } from '../../../shared/components/multi-select/multi-select.component';
import { downloadFileResponse, emptyToNull, optionalDate, toNumberArray } from '../report-utils';

@Component({
  selector: 'app-cancellation-debts-report',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './cancellation-debts-report.component.html',
  styleUrls: ['../report-page-shared.css']
})
export class CancellationDebtsReportComponent implements OnInit {
  private readonly locale = inject(LocaleService);
  private readonly reportClient = inject(AdminReportClient);
  private readonly customerClient = inject(AdminCustomerClient);
  private readonly router = inject(Router);

  report: CancellationDebtsReportDto | null = null;
  isLoading = false;
  isExporting = false;
  errorMessage = '';

  customerIds: number[] = [];
  states: number[] = [];
  fromDate = '';
  toDate = '';
  customerSearch = '';
  orderCode = '';
  customerOptions: MultiSelectOption[] = [];
  readonly ReportExportFormat = ReportExportFormat;

  get stateOptions(): MultiSelectOption[] {
    return [
      { value: CustomerWalletState.Pending, label: this.locale.translate('common.pending') },
      { value: CustomerWalletState.Paid, label: this.locale.translate('common.paid') },
      { value: CustomerWalletState.UnderPayment, label: this.locale.translate('reports.underPayment') }
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
  onStatesChange(v: Array<string | number | boolean>): void { this.states = toNumberArray(v); }

  clearFilters(): void {
    this.customerIds = [];
    this.states = [];
    this.fromDate = '';
    this.toDate = '';
    this.customerSearch = '';
    this.orderCode = '';
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.reportClient.getCancellationDebts(
      emptyToNull(this.customerIds),
      emptyToNull(this.states),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.customerSearch.trim() || null,
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
    this.reportClient.exportCancellationDebts(
      emptyToNull(this.customerIds),
      emptyToNull(this.states),
      optionalDate(this.fromDate),
      optionalDate(this.toDate),
      this.customerSearch.trim() || null,
      this.orderCode.trim() || null,
      format
    ).subscribe({
      next: (file) => {
        downloadFileResponse(file, `CancellationDebts.${format === ReportExportFormat.Pdf ? 'pdf' : 'xlsx'}`);
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

  stateLabel(state: CustomerWalletState): string {
    if (state === CustomerWalletState.Paid) return this.locale.translate('common.paid');
    if (state === CustomerWalletState.UnderPayment) return this.locale.translate('reports.underPayment');
    return this.locale.translate('common.pending');
  }

  stateClass(state: CustomerWalletState): string {
    if (state === CustomerWalletState.Paid) return 'rp__chip--ok';
    if (state === CustomerWalletState.UnderPayment) return 'rp__chip--info';
    return 'rp__chip--warn';
  }
}
