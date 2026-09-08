import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Observable, Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import {
  AdminHomeClient,
  HomeCancellationsDto,
  HomeCityPerformanceDto,
  HomeCustomerGrowthDto,
  HomeOrderPipelineDto,
  HomePaymentsMixDto,
  HomeRecentActivityDto,
  HomeRevenueTrendDto,
  HomeSummaryDto,
  HomeTopPerformersDto,
  HomeTreasurySnapshotDto
} from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { VoltChartComponent, VoltChartDataset } from '../../shared/components/volt-chart/volt-chart.component';

type SectionKey =
  | 'summary'
  | 'revenue'
  | 'pipeline'
  | 'customers'
  | 'payments'
  | 'treasury'
  | 'cancellations'
  | 'top'
  | 'cities'
  | 'recent';

interface DateRange {
  from: string;
  to: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, VoltChartComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly localeService = inject(LocaleService);
  private readonly subs = new Map<SectionKey, Subscription>();

  ranges: Record<Exclude<SectionKey, 'recent'>, DateRange> = {
    summary: this.defaultRange(),
    revenue: this.defaultRange(),
    pipeline: this.defaultRange(),
    customers: this.defaultRange(),
    payments: this.defaultRange(),
    treasury: this.defaultRange(),
    cancellations: this.defaultRange(),
    top: this.defaultRange(),
    cities: this.defaultRange()
  };

  loading: Partial<Record<SectionKey, boolean>> = {};

  summary: HomeSummaryDto | null = null;
  revenue: HomeRevenueTrendDto | null = null;
  pipeline: HomeOrderPipelineDto | null = null;
  customers: HomeCustomerGrowthDto | null = null;
  payments: HomePaymentsMixDto | null = null;
  treasury: HomeTreasurySnapshotDto | null = null;
  cancellations: HomeCancellationsDto | null = null;
  topPerformers: HomeTopPerformersDto | null = null;
  cityPerformance: HomeCityPerformanceDto | null = null;
  recentActivity: HomeRecentActivityDto | null = null;

  revenueLabels: string[] = [];
  revenueDatasets: VoltChartDataset[] = [];
  pipelineLabels: string[] = [];
  pipelineDatasets: VoltChartDataset[] = [];
  customerLabels: string[] = [];
  customerDatasets: VoltChartDataset[] = [];
  paymentLabels: string[] = [];
  paymentDatasets: VoltChartDataset[] = [];
  treasuryLabels: string[] = [];
  treasuryDatasets: VoltChartDataset[] = [];
  cancelLabels: string[] = [];
  cancelDatasets: VoltChartDataset[] = [];
  cityLabels: string[] = [];
  cityDatasets: VoltChartDataset[] = [];

  constructor(
    private homeClient: AdminHomeClient,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadSummary();
    this.loadRevenue();
    this.loadPipeline();
    this.loadCustomers();
    this.loadPayments();
    this.loadTreasury();
    this.loadCancellations();
    this.loadTop();
    this.loadCities();
    this.loadRecent();
  }

  ngOnDestroy(): void {
    this.subs.forEach(sub => sub.unsubscribe());
    this.subs.clear();
  }

  onRangeChange(section: Exclude<SectionKey, 'recent'>): void {
    switch (section) {
      case 'summary': this.loadSummary(); break;
      case 'revenue': this.loadRevenue(); break;
      case 'pipeline': this.loadPipeline(); break;
      case 'customers': this.loadCustomers(); break;
      case 'payments': this.loadPayments(); break;
      case 'treasury': this.loadTreasury(); break;
      case 'cancellations': this.loadCancellations(); break;
      case 'top': this.loadTop(); break;
      case 'cities': this.loadCities(); break;
    }
  }

  loadSummary(): void {
    const range = this.resolveRange(this.ranges.summary);
    if (!range) return;
    this.run('summary', this.homeClient.getSummary(range.from, range.to), data => {
      this.summary = data;
    });
  }

  loadRevenue(): void {
    const range = this.resolveRange(this.ranges.revenue);
    if (!range) return;
    this.run('revenue', this.homeClient.getRevenueTrend(range.from, range.to, null, this.granularityFor(range.from, range.to)), data => {
      this.revenue = data;
      const series = data.series || [];
      this.revenueLabels = series.map(p => p.period);
      this.revenueDatasets = [{
        label: this.localeService.translate('dashboard.revenue'),
        data: series.map(p => p.value || 0),
        color: '#1ec8b8',
        fill: true
      }];
    });
  }

  loadPipeline(): void {
    const range = this.resolveRange(this.ranges.pipeline);
    if (!range) return;
    this.run('pipeline', this.homeClient.getOrderPipeline(range.from, range.to), data => {
      this.pipeline = data;
      const states = data.byState || [];
      this.pipelineLabels = states.map(s => s.stateName || String(s.state));
      this.pipelineDatasets = [{
        label: this.localeService.translate('dashboard.orders'),
        data: states.map(s => s.count || 0)
      }];
    });
  }

  loadCustomers(): void {
    const range = this.resolveRange(this.ranges.customers);
    if (!range) return;
    this.run('customers', this.homeClient.getCustomerGrowth(range.from, range.to, null, this.granularityFor(range.from, range.to)), data => {
      this.customers = data;
      const series = data.series || [];
      this.customerLabels = series.map(p => p.period);
      this.customerDatasets = [{
        label: this.localeService.translate('dashboard.newCustomers'),
        data: series.map(p => p.count || p.value || 0),
        color: '#8b5cf6',
        fill: true
      }];
    });
  }

  loadPayments(): void {
    const range = this.resolveRange(this.ranges.payments);
    if (!range) return;
    this.run('payments', this.homeClient.getPaymentsMix(range.from, range.to), data => {
      this.payments = data;
      const methods = data.methods || [];
      this.paymentLabels = methods.map(m => m.methodName || String(m.methodId ?? ''));
      this.paymentDatasets = [{
        label: this.localeService.translate('dashboard.payments'),
        data: methods.map(m => m.amount || 0)
      }];
    });
  }

  loadTreasury(): void {
    const range = this.resolveRange(this.ranges.treasury);
    if (!range) return;
    this.run('treasury', this.homeClient.getTreasurySnapshot(range.from, range.to, this.granularityFor(range.from, range.to)), data => {
      this.treasury = data;
      const series = data.series || [];
      this.treasuryLabels = series.map(p => p.period);
      this.treasuryDatasets = [
        {
          label: this.localeService.translate('dashboard.debit'),
          data: series.map(p => p.debit || 0),
          color: '#ef4444'
        },
        {
          label: this.localeService.translate('dashboard.credit'),
          data: series.map(p => p.credit || 0),
          color: '#10b981'
        }
      ];
    });
  }

  loadCancellations(): void {
    const range = this.resolveRange(this.ranges.cancellations);
    if (!range) return;
    this.run('cancellations', this.homeClient.getCancellations(range.from, range.to, this.granularityFor(range.from, range.to)), data => {
      this.cancellations = data;
      const series = data.series || [];
      this.cancelLabels = series.map(p => p.period);
      this.cancelDatasets = [{
        label: this.localeService.translate('dashboard.cancellationFees'),
        data: series.map(p => p.value || 0),
        color: '#f59e0b',
        fill: true
      }];
    });
  }

  loadTop(): void {
    const range = this.resolveRange(this.ranges.top);
    if (!range) return;
    this.run('top', this.homeClient.getTopPerformers(range.from, range.to, null, 5), data => {
      this.topPerformers = data;
    });
  }

  loadCities(): void {
    const range = this.resolveRange(this.ranges.cities);
    if (!range) return;
    this.run('cities', this.homeClient.getCityPerformance(range.from, range.to), data => {
      this.cityPerformance = data;
      const cities = (data.cities || []).slice(0, 8);
      this.cityLabels = cities.map(c => c.cityName);
      this.cityDatasets = [{
        label: this.localeService.translate('dashboard.revenue'),
        data: cities.map(c => c.revenue || 0),
        color: '#1ec8b8'
      }];
    });
  }

  loadRecent(): void {
    this.run('recent', this.homeClient.getRecentActivity(10), data => {
      this.recentActivity = data;
    });
  }

  formatCurrency(amount: number | null | undefined): string {
    const currency = this.localeService.translate('common.currency');
    if (amount == null) return `0 ${currency}`;
    return `${new Intl.NumberFormat(this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-EG', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount)} ${currency}`;
  }

  formatNumber(value: number | null | undefined): string {
    if (value == null) return '0';
    return new Intl.NumberFormat(this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-EG').format(value);
  }

  formatPercent(value: number | null | undefined): string {
    if (value == null) return '0%';
    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(1)}%`;
  }

  formatDate(date: Date | string | null | undefined): string {
    if (!date) return '—';
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '—';
    return new Intl.DateTimeFormat(this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(d);
  }

  deltaClass(value: number | null | undefined): string {
    if (value == null || value === 0) return 'home-kpi__delta--flat';
    return value > 0 ? 'home-kpi__delta--up' : 'home-kpi__delta--down';
  }

  maxTopRevenue(): number {
    const items = [
      ...(this.topPerformers?.categories || []),
      ...(this.topPerformers?.subCategories || [])
    ];
    return Math.max(1, ...items.map(i => i.revenue || 0));
  }

  barWidth(revenue: number | null | undefined): number {
    return Math.max(4, Math.round(((revenue || 0) / this.maxTopRevenue()) * 100));
  }

  stateClass(stateName: string | null | undefined): string {
    const s = (stateName || '').toLowerCase();
    if (s.includes('pending')) return 'home-badge--pending';
    if (s.includes('confirm')) return 'home-badge--confirmed';
    if (s.includes('way') || s.includes('deliver')) return 'home-badge--onway';
    if (s.includes('receiv') || s.includes('rent')) return 'home-badge--active';
    if (s.includes('complete')) return 'home-badge--completed';
    if (s.includes('cancel')) return 'home-badge--cancelled';
    return 'home-badge--default';
  }

  private run<T>(key: SectionKey, source: Observable<T>, apply: (data: T) => void): void {
    this.subs.get(key)?.unsubscribe();
    this.loading[key] = true;
    this.subs.set(key, source.subscribe({
      next: data => {
        apply(data);
        this.loading[key] = false;
      },
      error: (err) => {
        this.loading[key] = false;
        console.error(`AdminHome/${key} failed`, err);
        this.notifyLoadError();
      }
    }));
  }

  private loadErrorNotified = false;

  private notifyLoadError(): void {
    if (this.loadErrorNotified) {
      return;
    }
    this.loadErrorNotified = true;
    this.toastr.error(
      this.localeService.translate('common.failedToLoad'),
      this.localeService.translate('common.error')
    );
    // Allow another toast on the next full refresh cycle.
    setTimeout(() => {
      this.loadErrorNotified = false;
    }, 4000);
  }

  private resolveRange(range: DateRange): { from: Date; to: Date } | null {
    const from = this.parseInputDate(range.from, false);
    const to = this.parseInputDate(range.to, true);
    if (!from || !to || from > to) {
      this.toastr.warning(
        this.localeService.translate('dashboard.invalidRange'),
        this.localeService.translate('common.error')
      );
      return null;
    }
    return { from, to };
  }

  private granularityFor(from: Date, to: Date): string {
    const days = Math.max(1, Math.round((to.getTime() - from.getTime()) / 86400000) + 1);
    if (days <= 45) return 'day';
    if (days <= 180) return 'week';
    return 'month';
  }

  private defaultRange(): DateRange {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - 29);
    return {
      from: this.toInputDate(from),
      to: this.toInputDate(to)
    };
  }

  private toInputDate(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private parseInputDate(value: string, endOfDay: boolean): Date | null {
    if (!value) return null;
    const parts = value.split('-').map(Number);
    if (parts.length !== 3 || parts.some(n => Number.isNaN(n))) return null;
    const date = endOfDay
      ? new Date(parts[0], parts[1] - 1, parts[2], 23, 59, 59, 999)
      : new Date(parts[0], parts[1] - 1, parts[2], 0, 0, 0, 0);
    return isNaN(date.getTime()) ? null : date;
  }
}
