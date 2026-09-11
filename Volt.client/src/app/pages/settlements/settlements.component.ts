import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminCollectFromDeliveryCommand,
  AdminCollectFromDeliveryKind,
  AdminPayDeliveryCommand,
  AdminPayDeliveryKind,
  AdminPayMerchantCommand,
  AdminSettlementClient,
  DeliveryClient,
  DeliveryLookupDto,
  JournalDirection,
  LedgerPartyType,
  MerchantClient,
  MerchantLookupDto,
  OrderJournalEntryKind,
  PartyLedgerDto
} from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

type SettlementTab = 'payDelivery' | 'collect' | 'payMerchant';

@Component({
  selector: 'app-settlements',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './settlements.component.html',
  styleUrls: ['./settlements.component.css', '../../shared/styles/entity-form.css']
})
export class SettlementsComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly settlementClient = inject(AdminSettlementClient);
  private readonly deliveryClient = inject(DeliveryClient);
  private readonly merchantClient = inject(MerchantClient);

  activeTab: SettlementTab = 'payDelivery';
  deliveries: DeliveryLookupDto[] = [];
  merchants: MerchantLookupDto[] = [];
  isLoadingLookups = false;
  isSubmitting = false;
  isLoadingLedger = false;
  errorMessage = '';
  successMessage = '';
  ledger: PartyLedgerDto | null = null;

  // Pay Delivery
  payDeliveryKind: AdminPayDeliveryKind = AdminPayDeliveryKind.CashFloat;
  payDeliveryId: number | null = null;
  payDeliveryAmount: number | null = null;
  payDeliveryOrderId: number | null = null;
  payDeliveryNote = '';

  // Collect
  collectKind: AdminCollectFromDeliveryKind = AdminCollectFromDeliveryKind.OrderCashRemittance;
  collectDeliveryId: number | null = null;
  collectAmount: number | null = null;
  collectOrderId: number | null = null;
  collectNote = '';

  // Pay Merchant
  payMerchantId: number | null = null;
  payMerchantOrderId: number | null = null;
  payMerchantAmount: number | null = null;
  payMerchantNote = '';

  readonly AdminPayDeliveryKind = AdminPayDeliveryKind;
  readonly AdminCollectFromDeliveryKind = AdminCollectFromDeliveryKind;

  ngOnInit(): void {
    this.loadLookups();
  }

  setTab(tab: SettlementTab): void {
    this.activeTab = tab;
    this.errorMessage = '';
    this.successMessage = '';
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

  get needsPayDeliveryOrderId(): boolean {
    return this.payDeliveryKind === AdminPayDeliveryKind.OrderPayout;
  }

  get needsCollectOrderId(): boolean {
    return this.collectKind === AdminCollectFromDeliveryKind.OrderCashRemittance;
  }

  onSubmitPayDelivery(): void {
    if (!this.payDeliveryId || !this.payDeliveryAmount || this.payDeliveryAmount <= 0) {
      this.showError(this.localeService.translate('settlements.validationRequired'));
      return;
    }
    if (this.needsPayDeliveryOrderId && !this.payDeliveryOrderId) {
      this.showError(this.localeService.translate('settlements.orderIdRequired'));
      return;
    }

    this.isSubmitting = true;
    const command = new AdminPayDeliveryCommand();
    command.deliveryId = this.payDeliveryId;
    command.kind = this.payDeliveryKind;
    command.amount = this.payDeliveryAmount;
    command.orderId = this.needsPayDeliveryOrderId ? this.payDeliveryOrderId : null;
    command.note = this.payDeliveryNote?.trim() || null;

    this.settlementClient.payDelivery(command).subscribe({
      next: (result) => {
        this.showSuccess(result?.message || this.localeService.translate('settlements.success'));
        this.loadLedger(LedgerPartyType.Delivery, this.payDeliveryId!);
        this.isSubmitting = false;
      },
      error: (error: any) => {
        this.showError(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('settlements.failed')
        );
        this.isSubmitting = false;
      }
    });
  }

  onSubmitCollect(): void {
    if (!this.collectDeliveryId || !this.collectAmount || this.collectAmount <= 0) {
      this.showError(this.localeService.translate('settlements.validationRequired'));
      return;
    }
    if (this.needsCollectOrderId && !this.collectOrderId) {
      this.showError(this.localeService.translate('settlements.orderIdRequired'));
      return;
    }

    this.isSubmitting = true;
    const command = new AdminCollectFromDeliveryCommand();
    command.deliveryId = this.collectDeliveryId;
    command.kind = this.collectKind;
    command.amount = this.collectAmount;
    command.orderId = this.needsCollectOrderId ? this.collectOrderId : null;
    command.note = this.collectNote?.trim() || null;
    command.reason = null;

    this.settlementClient.collectFromDelivery(command).subscribe({
      next: (result) => {
        this.showSuccess(result?.message || this.localeService.translate('settlements.success'));
        this.loadLedger(LedgerPartyType.Delivery, this.collectDeliveryId!);
        this.isSubmitting = false;
      },
      error: (error: any) => {
        this.showError(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('settlements.failed')
        );
        this.isSubmitting = false;
      }
    });
  }

  onSubmitPayMerchant(): void {
    if (!this.payMerchantId || !this.payMerchantOrderId) {
      this.showError(this.localeService.translate('settlements.validationRequired'));
      return;
    }

    this.isSubmitting = true;
    const command = new AdminPayMerchantCommand();
    command.merchantId = this.payMerchantId;
    command.orderId = this.payMerchantOrderId;
    command.amount = this.payMerchantAmount && this.payMerchantAmount > 0 ? this.payMerchantAmount : null;
    command.note = this.payMerchantNote?.trim() || null;

    this.settlementClient.payMerchant(command).subscribe({
      next: (result) => {
        this.showSuccess(result?.message || this.localeService.translate('settlements.success'));
        this.loadLedger(LedgerPartyType.Merchant, this.payMerchantId!);
        this.isSubmitting = false;
      },
      error: (error: any) => {
        this.showError(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('settlements.failed')
        );
        this.isSubmitting = false;
      }
    });
  }

  loadLedger(partyType: LedgerPartyType, partyId: number): void {
    this.isLoadingLedger = true;
    this.ledger = null;
    this.settlementClient.getLedger(partyType, partyId).subscribe({
      next: (ledger) => {
        this.ledger = ledger;
        this.isLoadingLedger = false;
      },
      error: (error: any) => {
        this.showError(
          error?.errorMessage || error?.error?.errorMessage || this.localeService.translate('settlements.ledgerFailed')
        );
        this.isLoadingLedger = false;
      }
    });
  }

  getJournalDirectionLabel(direction: JournalDirection): string {
    return direction === JournalDirection.Debit
      ? this.localeService.translate('orders.journalDebit')
      : this.localeService.translate('orders.journalCredit');
  }

  getJournalKindLabel(kind: OrderJournalEntryKind): string {
    const key = `orders.journalKind.${OrderJournalEntryKind[kind]}`;
    const translated = this.localeService.translate(key);
    return translated === key ? String(kind) : translated;
  }

  private showSuccess(message: string): void {
    this.successMessage = message;
    this.errorMessage = '';
    setTimeout(() => { this.successMessage = ''; }, 5000);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.successMessage = '';
    setTimeout(() => { this.errorMessage = ''; }, 6000);
  }
}
