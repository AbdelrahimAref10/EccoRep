import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MerchantClient, MerchantDashboardSummaryDto } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-home',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './merchant-home.component.html',
  styleUrls: [
    './merchant-home.component.css',
    '../dashboard/dashboard.component.css'
  ]
})
export class MerchantHomeComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);

  summary: MerchantDashboardSummaryDto | null = null;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.merchantClient.getDashboard().subscribe({
      next: (data) => {
        this.summary = data;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.loadFailed');
      }
    });
  }

  formatMoney(value: number | undefined): string {
    return `${Number(value || 0).toFixed(2)} ${this.localeService.translate('common.currency')}`;
  }
}
