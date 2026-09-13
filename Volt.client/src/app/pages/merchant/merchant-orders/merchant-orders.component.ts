import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subscription, debounceTime, filter } from 'rxjs';
import {
  MerchantClient,
  MerchantOrderResponseStatus,
  MerchantPortalOrderListItemDto,
  OrderState
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { MerchantNotificationService } from '../../../core/services/merchant-notification.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-merchant-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, PaginationComponent],
  templateUrl: './merchant-orders.component.html',
  styleUrls: [
    './merchant-orders.component.css',
    '../../orders/orders.component.css',
    '../../../shared/styles/list-filters.css'
  ]
})
export class MerchantOrdersComponent implements OnInit, OnDestroy {
  private readonly localeService = inject(LocaleService);
  private readonly merchantClient = inject(MerchantClient);
  private readonly merchantNotifications = inject(MerchantNotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private notificationSub?: Subscription;

  orders: MerchantPortalOrderListItemDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  isLoading = false;
  errorMessage = '';

  searchOrderCode = '';
  selectedState: OrderState | null = null;
  pendingOnly = false;
  awaitingHandoverOnly = false;

  readonly OrderState = OrderState;
  readonly pipelineStates: Array<{ state: OrderState | null; key: string }> = [
    { state: null, key: 'common.all' },
    { state: OrderState.MerchantPending, key: 'common.merchantPending' },
    { state: OrderState.MerchantConfirmed, key: 'common.merchantConfirmed' },
    { state: OrderState.Confirmed, key: 'common.confirmed' },
    { state: OrderState.DeliveryAssigned, key: 'common.deliveryAssigned' },
    { state: OrderState.OnWay, key: 'common.onWay' },
    { state: OrderState.CustomerReceived, key: 'common.received' },
    { state: OrderState.Completed, key: 'common.completed' },
    { state: OrderState.Cancelled, key: 'orders.cancelled' }
  ];

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      this.pendingOnly = params.get('pendingOnly') === 'true';
      this.awaitingHandoverOnly = params.get('awaitingHandover') === 'true';
      this.currentPage = 1;
      this.loadOrders();
    });

    this.notificationSub = this.merchantNotifications.incoming$
      .pipe(
        filter(notification => !!notification),
        debounceTime(300)
      )
      .subscribe(() => this.loadOrders(true));
  }

  ngOnDestroy(): void {
    this.notificationSub?.unsubscribe();
  }

  loadOrders(silent = false): void {
    if (!silent) {
      this.isLoading = true;
      this.errorMessage = '';
    }
    this.merchantClient
      .getMyOrders(
        this.currentPage,
        this.pageSize,
        this.selectedState ?? undefined,
        this.searchOrderCode.trim() || undefined,
        undefined,
        this.pendingOnly || undefined,
        this.awaitingHandoverOnly || undefined
      )
      .subscribe({
        next: (result) => {
          this.orders = result.items || [];
          this.totalCount = result.totalCount || 0;
          this.totalPages = result.totalPages || 0;
          this.isLoading = false;
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage =
            error?.errorMessage ||
            error?.error?.errorMessage ||
            this.localeService.translate('merchant.ordersLoadFailed');
        }
      });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  selectState(state: OrderState | null): void {
    this.selectedState = state;
    this.currentPage = 1;
    this.loadOrders();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadOrders();
  }

  clearQuickFilters(): void {
    this.pendingOnly = false;
    this.awaitingHandoverOnly = false;
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
    this.currentPage = 1;
    this.loadOrders();
  }

  openOrder(orderId: number): void {
    this.router.navigate(['/merchant/orders', orderId]);
  }

  getStateLabel(state: OrderState): string {
    const map: Record<number, string> = {
      [OrderState.Pending]: 'common.pending',
      [OrderState.MerchantPending]: 'common.merchantPending',
      [OrderState.MerchantConfirmed]: 'common.merchantConfirmed',
      [OrderState.Confirmed]: 'common.confirmed',
      [OrderState.DeliveryAssigned]: 'common.deliveryAssigned',
      [OrderState.OnWay]: 'common.onWay',
      [OrderState.CustomerReceived]: 'common.received',
      [OrderState.CustomerRejectedReceipt]: 'common.rejectedReceipt',
      [OrderState.Completed]: 'common.completed',
      [OrderState.Cancelled]: 'orders.cancelled'
    };
    return this.localeService.translate(map[state] || 'common.pending');
  }

  getResponseLabel(status: MerchantOrderResponseStatus): string {
    if (status === MerchantOrderResponseStatus.Accepted) {
      return this.localeService.translate('merchant.responseAccepted');
    }
    if (status === MerchantOrderResponseStatus.Rejected) {
      return this.localeService.translate('merchant.responseRejected');
    }
    if (status === MerchantOrderResponseStatus.PartiallyAccepted) {
      return this.localeService.translate('merchant.responsePartial');
    }
    return this.localeService.translate('merchant.responsePending');
  }

  formatMoney(value: number): string {
    return `${Number(value || 0).toFixed(2)} ${this.localeService.translate('common.currency')}`;
  }
}
