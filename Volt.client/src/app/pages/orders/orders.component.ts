import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs';
import { AdminOrderClient, OrderDto, PagedResultOfOrderDto, OrderState, PaymentMethod, CityClient, CityDto, PagedResultOfCityDto } from '../../core/services/clientAPI';
import { AdminNotificationService } from '../../core/services/admin-notification.service';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, MultiSelectComponent, PaginationComponent],
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css', '../../shared/styles/list-filters.css']
})
export class OrdersComponent implements OnInit, OnDestroy {
  private readonly localeService = inject(LocaleService);
  private readonly adminNotifications = inject(AdminNotificationService);

  /** Full list from API (before state/city client filters). */
  private allOrders: OrderDto[] = [];
  /** Current page of filtered orders shown in the UI. */
  orders: OrderDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  searchOrderCode = '';
  selectedState: OrderState | null = null;
  viewMode: 'table' | 'cards' = this.readStoredViewMode();
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  searchFilters = {
    state: null as OrderState | null,
    cityId: null as number | null
  };

  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  cities: CityDto[] = [];
  isLoadingCities = false;

  readonly pipelineStates: Array<{ state: OrderState | null; key: string; dot: string }> = [
    { state: null, key: 'common.all', dot: 'all' },
    { state: OrderState.Pending, key: 'common.pending', dot: 'pending' },
    { state: OrderState.MerchantPending, key: 'common.merchantPending', dot: 'merchant-pending' },
    { state: OrderState.MerchantConfirmed, key: 'common.merchantConfirmed', dot: 'merchant-confirmed' },
    { state: OrderState.Confirmed, key: 'common.confirmed', dot: 'confirmed' },
    { state: OrderState.DeliveryAssigned, key: 'common.deliveryAssigned', dot: 'delivery-assigned' },
    { state: OrderState.OnWay, key: 'common.onWay', dot: 'onway' },
    { state: OrderState.CustomerReceived, key: 'common.received', dot: 'received' },
    { state: OrderState.CustomerRejectedReceipt, key: 'common.rejectedReceipt', dot: 'rejected-receipt' },
    { state: OrderState.Completed, key: 'common.completed', dot: 'completed' },
    { state: OrderState.Cancelled, key: 'orders.cancelled', dot: 'cancelled' }
  ];

  get stateOptions(): MultiSelectOption[] {
    return [
      { value: OrderState.Pending, label: this.localeService.translate('common.pending') },
      { value: OrderState.MerchantPending, label: this.localeService.translate('common.merchantPending') },
      { value: OrderState.MerchantConfirmed, label: this.localeService.translate('common.merchantConfirmed') },
      { value: OrderState.Confirmed, label: this.localeService.translate('common.confirmed') },
      { value: OrderState.DeliveryAssigned, label: this.localeService.translate('common.deliveryAssigned') },
      { value: OrderState.OnWay, label: this.localeService.translate('common.onWay') },
      { value: OrderState.CustomerReceived, label: this.localeService.translate('common.received') },
      { value: OrderState.CustomerRejectedReceipt, label: this.localeService.translate('common.rejectedReceipt') },
      { value: OrderState.Completed, label: this.localeService.translate('common.completed') },
      { value: OrderState.Cancelled, label: this.localeService.translate('orders.cancelled') }
    ];
  }

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(city => ({
      value: city.cityId,
      label: city.name
    }));
  }

  constructor(
    private orderClient: AdminOrderClient,
    private cityClient: CityClient,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCities();
    this.setupLiveSearch();
    this.triggerSearch();

    this.adminNotifications.incoming$
      .pipe(takeUntil(this.destroy$))
      .subscribe(notification => {
        if (notification) {
          this.triggerSearch();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.searchSubject.complete();
  }

  setupLiveSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((orderCode) => {
        this.isLoading = true;
        this.errorMessage = '';
        this.successMessage = '';

        // Load all matching orders (no state filter) — state/city filtering is client-side.
        return this.orderClient.getAllOrders(
          1,
          1000,
          undefined,
          orderCode || undefined
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (result: PagedResultOfOrderDto) => {
        this.allOrders = result.items || [];
        this.applyClientFilters();
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading orders:', error);
      }
    });
  }

  /** Apply state + city filters and paginate on the frontend. */
  private applyClientFilters(): void {
    let filtered = [...this.allOrders];

    const stateFilter = this.searchFilters.state !== null
      ? this.searchFilters.state
      : this.selectedState;

    if (stateFilter !== null) {
      filtered = filtered.filter(order => order.orderState === stateFilter);
    }

    if (this.searchFilters.cityId !== null && this.searchFilters.cityId > 0) {
      filtered = filtered.filter(order => order.cityId === this.searchFilters.cityId);
    }

    this.totalCount = filtered.length;
    this.totalPages = Math.max(1, Math.ceil(filtered.length / this.pageSize));

    if (this.currentPage > this.totalPages) {
      this.currentPage = this.totalPages;
    }

    const start = (this.currentPage - 1) * this.pageSize;
    this.orders = filtered.slice(start, start + this.pageSize);
  }

  loadCities(): void {
    this.isLoadingCities = true;
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
        this.isLoadingCities = false;
      },
      error: (error: any) => {
        console.error('Error loading cities:', error);
        this.isLoadingCities = false;
      }
    });
  }

  loadOrders(): void {
    this.triggerSearch();
  }

  triggerSearch(): void {
    this.searchSubject.next(this.searchOrderCode);
  }

  onSearch(): void {
    this.currentPage = 1;
    this.triggerSearch();
  }

  onStateFilter(state: OrderState | null): void {
    this.selectedState = state;
    this.searchFilters.state = null;
    this.currentPage = 1;
    // State filtering is client-side — no API round-trip needed.
    this.applyClientFilters();
  }

  onAdvancedSearch(): void {
    this.currentPage = 1;
    this.triggerSearch();
  }

  onSearchTermChange(): void {
    this.currentPage = 1;
    this.triggerSearch();
  }

  onFilterChange(): void {
    this.currentPage = 1;
    // City filter is client-side.
    this.applyClientFilters();
  }

  onClearSearch(): void {
    this.searchFilters = {
      state: null,
      cityId: null
    };
    this.searchOrderCode = '';
    this.selectedState = null;
    this.currentPage = 1;
    this.loadOrders();
  }

  hasActiveFilters(): boolean {
    return this.searchFilters.state !== null ||
           (this.searchFilters.cityId !== null && this.searchFilters.cityId > 0) ||
           (this.searchOrderCode && this.searchOrderCode.trim().length > 0) ||
           this.selectedState !== null;
  }

  onView(orderId: number): void {
    this.router.navigate(['/main/orders', orderId]);
  }

  onAddNew(): void {
    this.router.navigate(['/main/orders/new']);
  }

  setViewMode(mode: 'table' | 'cards'): void {
    if (this.viewMode === mode) {
      return;
    }
    this.viewMode = mode;
    localStorage.setItem('volt-orders-view', mode);
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.applyClientFilters();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  getStateLabel(state: OrderState): string {
    switch (state) {
      case OrderState.Pending:
        return this.localeService.translate('common.pending');
      case OrderState.MerchantPending:
        return this.localeService.translate('common.merchantPending');
      case OrderState.MerchantConfirmed:
        return this.localeService.translate('common.merchantConfirmed');
      case OrderState.Confirmed:
        return this.localeService.translate('common.confirmed');
      case OrderState.DeliveryAssigned:
        return this.localeService.translate('common.deliveryAssigned');
      case OrderState.OnWay:
        return this.localeService.translate('common.onWay');
      case OrderState.CustomerReceived:
        return this.localeService.translate('common.received');
      case OrderState.CustomerRejectedReceipt:
        return this.localeService.translate('common.rejectedReceipt');
      case OrderState.Completed:
        return this.localeService.translate('common.completed');
      case OrderState.Cancelled:
        return this.localeService.translate('orders.cancelled');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getStateClass(state: OrderState): string {
    switch (state) {
      case OrderState.Pending:
        return 'orders__state--pending';
      case OrderState.MerchantPending:
        return 'orders__state--merchant-pending';
      case OrderState.MerchantConfirmed:
        return 'orders__state--merchant-confirmed';
      case OrderState.Confirmed:
        return 'orders__state--confirmed';
      case OrderState.DeliveryAssigned:
        return 'orders__state--delivery-assigned';
      case OrderState.OnWay:
        return 'orders__state--onway';
      case OrderState.CustomerReceived:
        return 'orders__state--received';
      case OrderState.CustomerRejectedReceipt:
        return 'orders__state--rejected-receipt';
      case OrderState.Completed:
        return 'orders__state--completed';
      case OrderState.Cancelled:
        return 'orders__state--cancelled';
      default:
        return '';
    }
  }

  getPaymentMethodLabel(method: PaymentMethod): string {
    switch (method) {
      case PaymentMethod.Cash:
        return this.localeService.translate('common.cash');
      case PaymentMethod.PayPal:
        return this.localeService.translate('common.paypal');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getPaymentMethodClass(method: PaymentMethod): string {
    switch (method) {
      case PaymentMethod.Cash:
        return 'orders__payment--cash';
      case PaymentMethod.PayPal:
        return 'orders__payment--paypal';
      default:
        return '';
    }
  }

  showSuccessMessage(message: string): void {
    this.successMessage = message;
    this.errorMessage = '';
    setTimeout(() => {
      this.successMessage = '';
    }, 5000);
  }

  showErrorMessage(message: string): void {
    this.errorMessage = message;
    this.successMessage = '';
    setTimeout(() => {
      this.errorMessage = '';
    }, 5000);
  }

  getPipelineCount(state: OrderState | null): number {
    let source = this.allOrders;

    if (this.searchFilters.cityId !== null && this.searchFilters.cityId > 0) {
      source = source.filter(o => o.cityId === this.searchFilters.cityId);
    }

    if (state === null) {
      return source.length;
    }
    return source.filter(o => o.orderState === state).length;
  }

  get pendingOrdersCount(): number {
    return this.allOrders.filter(o => o.orderState === OrderState.Pending).length;
  }

  get confirmedOrdersCount(): number {
    return this.allOrders.filter(o => o.orderState === OrderState.Confirmed).length;
  }

  get onWayOrdersCount(): number {
    return this.allOrders.filter(o => o.orderState === OrderState.OnWay).length;
  }

  get receivedOrdersCount(): number {
    return this.allOrders.filter(o => o.orderState === OrderState.CustomerReceived).length;
  }

  get completedOrdersCount(): number {
    return this.allOrders.filter(o => o.orderState === OrderState.Completed).length;
  }

  private readStoredViewMode(): 'table' | 'cards' {
    const stored = localStorage.getItem('volt-orders-view');
    return stored === 'cards' || stored === 'table' ? stored : 'table';
  }
}
