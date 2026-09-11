import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  JournalDirection,
  MerchantClient,
  OrderJournalEntryKind,
  OrderJournalListDto,
  OrderJournalMovementDto,
  PartyLedgerDto
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-payments',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './merchant-payments.component.html',
  styleUrls: [
    './merchant-payments.component.css',
    '../../journals/journals.component.css',
    '../../../shared/styles/entity-form.css'
  ]
})
export class MerchantPaymentsComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);

  ledger: PartyLedgerDto | null = null;
  journals: OrderJournalListDto | null = null;
  isLoading = false;
  errorMessage = '';
  filterOrderId: number | null = null;

  readonly JournalDirection = JournalDirection;

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.isLoading = true;
    this.errorMessage = '';
    let pending = 2;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.isLoading = false;
    };

    this.merchantClient.getMyLedger().subscribe({
      next: (data) => {
        this.ledger = data;
        done();
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.paymentsLoadFailed');
        done();
      }
    });

    this.loadJournals(done);
  }

  loadJournals(done?: () => void): void {
    const orderId = this.filterOrderId && this.filterOrderId > 0 ? this.filterOrderId : undefined;
    this.merchantClient.getMyJournals(orderId).subscribe({
      next: (data) => {
        this.journals = data;
        done?.();
      },
      error: (error: any) => {
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('merchant.paymentsLoadFailed');
        done?.();
      }
    });
  }

  applyOrderFilter(): void {
    this.isLoading = true;
    this.loadJournals(() => (this.isLoading = false));
  }

  clearOrderFilter(): void {
    this.filterOrderId = null;
    this.applyOrderFilter();
  }

  get entries(): OrderJournalMovementDto[] {
    return this.journals?.entries ?? [];
  }

  getJournalKindLabel(kind: OrderJournalEntryKind): string {
    const key = `orders.journalKind.${OrderJournalEntryKind[kind]}`;
    const translated = this.localeService.translate(key);
    return translated !== key ? translated : String(kind);
  }

  formatMoney(value: number | undefined): string {
    return `${Number(value || 0).toFixed(2)} ${this.localeService.translate('common.currency')}`;
  }
}
