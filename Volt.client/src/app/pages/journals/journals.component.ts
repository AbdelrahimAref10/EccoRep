import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminSettlementClient,
  DeliveryClient,
  DeliveryLookupDto,
  JournalDirection,
  LedgerPartyType,
  MerchantClient,
  MerchantLookupDto,
  OrderJournalEntryKind,
  OrderJournalListDto,
  OrderJournalMovementDto
} from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-journals',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, MultiSelectComponent],
  templateUrl: './journals.component.html',
  styleUrls: ['./journals.component.css', '../../shared/styles/entity-form.css']
})
export class JournalsComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly settlementClient = inject(AdminSettlementClient);
  private readonly deliveryClient = inject(DeliveryClient);
  private readonly merchantClient = inject(MerchantClient);

  deliveries: DeliveryLookupDto[] = [];
  merchants: MerchantLookupDto[] = [];
  isLoadingLookups = false;
  isLoading = false;
  errorMessage = '';

  filterOrderCode = '';
  filterDeliveryId: number | null = null;
  filterMerchantId: number | null = null;

  result: OrderJournalListDto | null = null;

  readonly JournalDirection = JournalDirection;
  readonly LedgerPartyType = LedgerPartyType;

  get deliveryOptions(): MultiSelectOption[] {
    return this.deliveries
      .filter(d => d.deliveryId != null)
      .map(d => ({
        value: d.deliveryId as number,
        label: d.fullName || String(d.deliveryId),
        description: d.mobileNumber || '—'
      }));
  }

  get merchantOptions(): MultiSelectOption[] {
    return this.merchants
      .filter(m => m.merchantId != null)
      .map(m => ({
        value: m.merchantId as number,
        label: m.fullName || String(m.merchantId),
        description: m.mobileNumber || '—'
      }));
  }

  ngOnInit(): void {
    this.loadLookups();
    this.loadJournals();
  }

  loadLookups(): void {
    this.isLoadingLookups = true;
    let pending = 2;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.isLoadingLookups = false;
    };

    this.deliveryClient.getActive().subscribe({
      next: (list) => {
        this.deliveries = list || [];
        done();
      },
      error: () => done()
    });

    this.merchantClient.getActive().subscribe({
      next: (list) => {
        this.merchants = list || [];
        done();
      },
      error: () => done()
    });
  }

  onDeliveryChange(): void {
    if (this.filterDeliveryId != null) {
      this.filterMerchantId = null;
    }
  }

  onMerchantChange(): void {
    if (this.filterMerchantId != null) {
      this.filterDeliveryId = null;
    }
  }

  clearFilters(): void {
    this.filterOrderCode = '';
    this.filterDeliveryId = null;
    this.filterMerchantId = null;
    this.loadJournals();
  }

  loadJournals(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const orderCode = this.filterOrderCode.trim() || undefined;
    const deliveryId = this.filterDeliveryId && this.filterDeliveryId > 0 ? this.filterDeliveryId : undefined;
    const merchantId = this.filterMerchantId && this.filterMerchantId > 0 ? this.filterMerchantId : undefined;

    this.settlementClient.getJournals(orderCode, deliveryId, merchantId).subscribe({
      next: (data) => {
        this.result = data;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.result = null;
        this.isLoading = false;
        this.errorMessage =
          error?.errorMessage ||
          error?.error?.errorMessage ||
          this.localeService.translate('journals.loadFailed');
      }
    });
  }

  get entries(): OrderJournalMovementDto[] {
    return this.result?.entries ?? [];
  }

  get totalCredit(): number {
    return this.result?.totalCredit ?? 0;
  }

  get totalDebit(): number {
    return this.result?.totalDebit ?? 0;
  }

  get balance(): number {
    return this.result?.balance ?? 0;
  }

  getJournalKindLabel(kind: OrderJournalEntryKind): string {
    const key = `orders.journalKind.${OrderJournalEntryKind[kind]}`;
    const translated = this.localeService.translate(key);
    return translated !== key ? translated : String(kind);
  }

  getJournalDirectionLabel(direction: JournalDirection): string {
    return direction === JournalDirection.Debit
      ? this.localeService.translate('orders.journalDebit')
      : this.localeService.translate('orders.journalCredit');
  }

  getPartyTypeLabel(partyType: LedgerPartyType): string {
    if (partyType === LedgerPartyType.Merchant) {
      return this.localeService.translate('settlements.merchant');
    }
    if (partyType === LedgerPartyType.Delivery) {
      return this.localeService.translate('settlements.delivery');
    }
    return this.localeService.translate('journals.company');
  }

  formatMoney(value: number): string {
    return `${Number(value || 0).toFixed(2)} ${this.localeService.translate('common.currency')}`;
  }
}
