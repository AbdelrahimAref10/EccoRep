import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  AdminCustomerClient,
  CustomerDto,
  PagedResultOfCustomerDto,
  CustomerState,
  CityClient,
  CityDto,
  PagedResultOfCityDto
} from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslatePipe,
    MultiSelectComponent,
    PaginationComponent
  ],
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.css', '../../shared/styles/list-filters.css']
})
export class CustomersComponent implements OnInit, OnDestroy {
  private customerClient = inject(AdminCustomerClient);
  private cityClient = inject(CityClient);
  private router = inject(Router);
  private localeService = inject(LocaleService);

  customers: CustomerDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  searchTerm = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  selectedStateValues: Array<string | number | boolean> = [];
  selectedCityIds: Array<string | number | boolean> = [];
  selectedTypeValues: Array<string | number | boolean> = [];

  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  cities: CityDto[] = [];

  get stateOptions(): MultiSelectOption[] {
    return [
      { value: CustomerState.InActive, label: this.localeService.translate('common.inactive') },
      { value: CustomerState.Active, label: this.localeService.translate('common.active') },
      { value: CustomerState.Blocked, label: this.localeService.translate('common.blocked') }
    ];
  }

  get typeOptions(): MultiSelectOption[] {
    return [
      { value: 0, label: this.localeService.translate('common.individual') },
      { value: 1, label: this.localeService.translate('common.institution') }
    ];
  }

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(city => ({
      value: city.cityId,
      label: city.name
    }));
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.searchTerm.trim()) count++;
    count += this.selectedStateValues.length;
    count += this.selectedCityIds.length;
    count += this.selectedTypeValues.length;
    return count;
  }

  ngOnInit(): void {
    this.loadCities();
    this.setupLiveSearch();
    this.loadCustomers();
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
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadCustomers();
    });
  }

  loadCustomers(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const state = this.toNullableNumber(this.selectedStateValues) as CustomerState | undefined;
    const cityId = this.toNullableNumber(this.selectedCityIds);
    const registerAs = this.toNullableNumber(this.selectedTypeValues);

    this.customerClient.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      state,
      cityId,
      registerAs
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result: PagedResultOfCustomerDto) => {
        this.customers = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading customers:', error);
      }
    });
  }

  loadCities(): void {
    this.cityClient.getAll(1, 1000, undefined, true).subscribe({
      next: (result: PagedResultOfCityDto) => {
        this.cities = result.items || [];
      },
      error: (error: unknown) => {
        console.error('Error loading cities:', error);
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadCustomers();
  }

  onSearchTermChange(): void {
    this.searchSubject.next(this.searchTerm.trim());
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStateValues = [];
    this.selectedCityIds = [];
    this.selectedTypeValues = [];
    this.currentPage = 1;
    this.loadCustomers();
  }

  onStateChange(values: Array<string | number | boolean>): void {
    this.selectedStateValues = values;
  }

  onCityIdsChange(values: Array<string | number | boolean>): void {
    this.selectedCityIds = values;
  }

  onTypeChange(values: Array<string | number | boolean>): void {
    this.selectedTypeValues = values;
  }

  onAddNew(): void {
    this.router.navigate(['/main/customers/new']);
  }

  onView(customerId: number): void {
    this.router.navigate(['/main/customers', customerId]);
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadCustomers();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  getStateLabel(state: CustomerState): string {
    switch (state) {
      case CustomerState.Active:
        return this.localeService.translate('common.active');
      case CustomerState.InActive:
        return this.localeService.translate('common.inactive');
      case CustomerState.Blocked:
        return this.localeService.translate('common.blocked');
      default:
        return this.localeService.translate('common.noData');
    }
  }

  getTypeLabel(registerAs: number | undefined | null): string {
    return registerAs === 1
      ? this.localeService.translate('common.institution')
      : this.localeService.translate('common.individual');
  }

  getStateClass(state: CustomerState): string {
    switch (state) {
      case CustomerState.Active:
        return 'customers__status--active';
      case CustomerState.InActive:
        return 'customers__status--inactive';
      case CustomerState.Blocked:
        return 'customers__status--blocked';
      default:
        return '';
    }
  }

  private toNullableNumber(values: Array<string | number | boolean>): number | undefined {
    if (values.length !== 1) {
      return undefined;
    }
    const num = Number(values[0]);
    return Number.isNaN(num) ? undefined : num;
  }
}
