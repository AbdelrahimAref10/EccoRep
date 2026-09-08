import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  registerables
} from 'chart.js';
import { LocaleService } from '../../../core/services/locale.service';

Chart.register(...registerables);

export interface VoltChartDataset {
  label: string;
  data: number[];
  color?: string;
  fill?: boolean;
  tension?: number;
}

@Component({
  selector: 'volt-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="volt-chart" [class.volt-chart--empty]="!hasData">
      <canvas #canvas></canvas>
      <div class="volt-chart__empty" *ngIf="!hasData">{{ emptyLabel }}</div>
    </div>
  `,
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    .volt-chart {
      position: relative;
      width: 100%;
      height: 100%;
      min-height: 14rem;
    }
    .volt-chart canvas {
      width: 100% !important;
      height: 100% !important;
    }
    .volt-chart__empty {
      position: absolute;
      inset: 0;
      display: grid;
      place-items: center;
      color: var(--volt-text-muted);
      font-size: 0.875rem;
      font-weight: 600;
    }
  `]
})
export class VoltChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  @Input() type: ChartType = 'line';
  @Input() labels: string[] = [];
  @Input() datasets: VoltChartDataset[] = [];
  @Input() emptyLabel = '—';
  @Input() currency = false;
  @Input() legend = true;
  @Input() horizontal = false;

  private readonly localeService = inject(LocaleService);
  private chart: Chart | null = null;
  private viewReady = false;

  get hasData(): boolean {
    return this.labels.length > 0
      && this.datasets.some(d => d.data.some(v => v != null && Number(v) !== 0));
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.render();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.viewReady && (changes['labels'] || changes['datasets'] || changes['type'] || changes['horizontal'])) {
      this.render();
    }
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }

  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
  }

  private cssVar(name: string, fallback: string): string {
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return value || fallback;
  }

  private render(): void {
    this.destroyChart();
    if (!this.canvasRef?.nativeElement || !this.hasData) {
      return;
    }

    const accent = this.cssVar('--volt-accent', '#1ec8b8');
    const textMuted = this.cssVar('--volt-text-muted', '#92a3a1');
    const textSecondary = this.cssVar('--volt-text-secondary', '#667877');
    const border = this.cssVar('--volt-border', '#e6ecec');
    const palette = [
      accent,
      '#3b82f6',
      '#f59e0b',
      '#ef4444',
      '#8b5cf6',
      '#10b981',
      '#06b6d4',
      '#f97316'
    ];

    const chartDatasets = this.datasets.map((ds, index) => {
      const color = ds.color || palette[index % palette.length];
      const isLineLike = this.type === 'line' || this.type === 'radar';
      return {
        label: ds.label,
        data: ds.data,
        backgroundColor: this.type === 'doughnut' || this.type === 'pie'
          ? this.datasets.length === 1
            ? ds.data.map((_, i) => palette[i % palette.length])
            : this.withAlpha(color, 0.85)
          : this.type === 'bar'
            ? this.withAlpha(color, 0.85)
            : this.withAlpha(color, 0.18),
        borderColor: color,
        borderWidth: isLineLike ? 2.5 : 1.5,
        fill: ds.fill ?? (this.type === 'line'),
        tension: ds.tension ?? 0.35,
        pointRadius: isLineLike ? 3 : 0,
        pointHoverRadius: isLineLike ? 5 : 0,
        borderRadius: this.type === 'bar' ? 8 : 0,
        maxBarThickness: 36
      };
    });

    const config: ChartConfiguration = {
      type: this.type,
      data: {
        labels: this.labels,
        datasets: chartDatasets as ChartConfiguration['data']['datasets']
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: this.horizontal ? 'y' : 'x',
        plugins: {
          legend: {
            display: this.legend,
            position: 'bottom',
            labels: {
              color: textSecondary,
              boxWidth: 10,
              boxHeight: 10,
              usePointStyle: true,
              padding: 16,
              font: { family: 'var(--volt-font)', size: 12 }
            }
          },
          tooltip: {
            backgroundColor: this.cssVar('--volt-surface', '#fff'),
            titleColor: this.cssVar('--volt-text-primary', '#1a2b2a'),
            bodyColor: textSecondary,
            borderColor: border,
            borderWidth: 1,
            padding: 12,
            cornerRadius: 10,
            callbacks: this.currency
              ? {
                  label: (ctx) => {
                    const label = ctx.dataset.label ? `${ctx.dataset.label}: ` : '';
                    const value = Number(ctx.raw ?? 0);
                    const currencyLabel = this.localeService.translate('common.currency');
                    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-EG';
                    return `${label}${new Intl.NumberFormat(locale, {
                      minimumFractionDigits: 0,
                      maximumFractionDigits: 0
                    }).format(value)} ${currencyLabel}`;
                  }
                }
              : undefined
          }
        },
        scales: this.type === 'doughnut' || this.type === 'pie'
          ? undefined
          : {
              x: {
                grid: { color: this.withAlpha(border, 0.7) },
                border: { display: false },
                ticks: { color: textMuted, font: { family: 'var(--volt-font)', size: 11 }, maxRotation: 0 }
              },
              y: {
                beginAtZero: true,
                grid: { color: this.withAlpha(border, 0.7) },
                border: { display: false },
                ticks: {
                  color: textMuted,
                  font: { family: 'var(--volt-font)', size: 11 },
                  callback: this.currency
                    ? (value: string | number) => `${new Intl.NumberFormat('en-EG', { notation: 'compact' }).format(Number(value))}`
                    : undefined
                }
              }
            },
        cutout: this.type === 'doughnut' ? '68%' : undefined
      } as ChartConfiguration['options']
    };

    this.chart = new Chart(this.canvasRef.nativeElement, config);
  }

  private withAlpha(color: string, alpha: number): string {
    if (color.startsWith('#')) {
      const hex = color.slice(1);
      const full = hex.length === 3 ? hex.split('').map(c => c + c).join('') : hex;
      const r = parseInt(full.slice(0, 2), 16);
      const g = parseInt(full.slice(2, 4), 16);
      const b = parseInt(full.slice(4, 6), 16);
      return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
    return color;
  }
}
