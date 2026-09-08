import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  Subject,
  distinctUntilChanged,
  finalize,
  forkJoin,
  of,
  takeUntil
} from 'rxjs';
import {
  AdminAvailableVehicleItemDto,
  AdminAvailableVehiclesDto,
  AdminCalculateOrderTotalsQuery,
  AdminCreateOrderCommand,
  AdminCustomerClient,
  AdminOrderClient,
  AdminOrderTotalsPreviewDto,
  AdminUpdateOrderCommand,
  CategoryClient,
  CategoryDto,
  CityClient,
  CityDto,
  CustomerLookupDto,
  CustomerState,
  OrderDetailDto,
  OrderState,
  PaymentMethod,
  PaymentState,
  SubCategoryClient,
  SubCategoryDto
} from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';

type BrowseStep = 'categories' | 'subcategories' | 'vehicles';

interface CalendarDay {
  date: Date;
  day: number;
  inMonth: boolean;
  disabled: boolean;
  isToday: boolean;
  isStart: boolean;
  isEnd: boolean;
  inRange: boolean;
}

interface BookedCalendarDay {
  date: Date;
  day: number;
  inMonth: boolean;
  isBooked: boolean;
}

@Component({
  selector: 'app-order-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    TranslatePipe,
    MultiSelectComponent
  ],
  templateUrl: './order-form.component.html',
  styleUrls: [
    './order-form.component.css',
    '../../../shared/styles/entity-form.css',
    '../../../shared/styles/entity-tiles.css'
  ]
})
export class OrderFormComponent implements OnInit, OnDestroy {
  private readonly localeService = inject(LocaleService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderClient = inject(AdminOrderClient);
  private readonly customerClient = inject(AdminCustomerClient);
  private readonly cityClient = inject(CityClient);
  private readonly categoryClient = inject(CategoryClient);
  private readonly subCategoryClient = inject(SubCategoryClient);
  private readonly destroy$ = new Subject<void>();

  @ViewChild('passportInput') passportInputRef?: ElementRef<HTMLInputElement>;

  isEditMode = false;
  orderId: number | null = null;
  orderCode = '';
  isBootstrapping = true;
  isSaving = false;
  errorMessage = '';

  cities: CityDto[] = [];
  selectedCustomer: CustomerLookupDto | null = null;
  customerPhoneQuery = '';
  customerSearchResults: CustomerLookupDto[] = [];
  isSearchingCustomer = false;
  customerSearchError = '';
  customerSearchAttempted = false;
  cityAutoFilled = false;

  passportPreview: string | null = null;
  existingPassportImage: string | null = null;
  passportDirty = false;
  minDate = this.formatDateInput(new Date());

  orderForm: FormGroup;
  browseStep: BrowseStep = 'categories';
  isLoadingCategories = false;
  isLoadingSubCategories = false;
  isLoadingVehicles = false;
  isCalculating = false;

  categories: CategoryDto[] = [];
  selectedCategory: CategoryDto | null = null;
  subCategories: SubCategoryDto[] = [];
  selectedSubCategory: SubCategoryDto | null = null;

  availableFleet: AdminAvailableVehiclesDto | null = null;
  fleetVehicles: AdminAvailableVehicleItemDto[] = [];
  selectedVehicles: AdminAvailableVehicleItemDto[] = [];

  /** Month shown in the range calendar (first day of month). */
  calendarMonth = this.startOfMonth(new Date());
  /** Committed range (synced to the form). */
  rangeFrom: Date | null = null;
  rangeTo: Date | null = null;
  /** First click while building a new range (before To is chosen). */
  rangeAnchor: Date | null = null;
  hoverDate: Date | null = null;
  calendarDays: CalendarDay[] = [];
  nextCalendarDays: CalendarDay[] = [];

  isPreviewOpen = false;
  previewData: AdminOrderTotalsPreviewDto | null = null;
  modalError = '';

  /** Mini calendar for reserved vehicle conflicting dates. */
  bookedDaysVehicle: AdminAvailableVehicleItemDto | null = null;
  bookedCalendarMonth: Date = this.startOfMonth(new Date());

  editForm: FormGroup;
  editSubCategories: SubCategoryDto[] = [];

  constructor() {
    this.orderForm = this.fb.group({
      customerId: [null as number | null, Validators.required],
      cityId: [null as number | null, Validators.required],
      reservationDateFrom: ['', Validators.required],
      reservationDateTo: ['', Validators.required],
      hotelName: ['', [Validators.required, Validators.maxLength(200)]],
      hotelAddress: ['', [Validators.required, Validators.maxLength(500)]],
      hotelPhone: [''],
      notes: [''],
      isUrgent: [false]
    });

    this.editForm = this.fb.group({
      customerId: [null as number | null, Validators.required],
      cityId: [null as number | null, Validators.required],
      subCategoryId: [null as number | null, Validators.required],
      reservationDateFrom: ['', Validators.required],
      reservationDateTo: ['', Validators.required],
      vehiclesCount: [1, [Validators.required, Validators.min(1)]],
      hotelName: ['', [Validators.required, Validators.maxLength(200)]],
      hotelAddress: ['', [Validators.required, Validators.maxLength(500)]],
      hotelPhone: [''],
      notes: [''],
      isUrgent: [false]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.orderId = +id;
      this.isEditMode = true;
    }

    this.bindFormListeners();
    this.loadLookups();
    this.refreshCalendar();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get cityOptions(): MultiSelectOption[] {
    return this.cities.map(c => ({
      value: c.cityId,
      label: c.name
    }));
  }

  get selectedCustomerCashBlocked(): boolean {
    return !!this.selectedCustomer?.cashBlock;
  }

  get canSearchCustomer(): boolean {
    return this.countPhoneDigits(this.customerPhoneQuery) >= 4 && !this.isSearchingCustomer;
  }

  get selectedVehicleCount(): number {
    return this.selectedVehicles.length;
  }

  get selectedVehicleIds(): number[] {
    return this.selectedVehicles.map(v => v.vehicleId);
  }

  get reservationDays(): number {
    if (!this.rangeFrom || !this.rangeTo || this.rangeFrom > this.rangeTo) return 0;
    return Math.max(1, Math.round((this.rangeTo.getTime() - this.rangeFrom.getTime()) / 86400000) + 1);
  }

  get hasCompleteRange(): boolean {
    return !!(this.rangeFrom && this.rangeTo && this.rangeFrom <= this.rangeTo);
  }

  get tripDetailsValid(): boolean {
    if (this.orderForm.invalid) return false;
    return this.hasCompleteRange && this.passportDirty;
  }

  get canReview(): boolean {
    return !this.isEditMode
      && this.tripDetailsValid
      && !!this.selectedSubCategory
      && this.selectedVehicleCount > 0
      && !this.selectedCustomerCashBlocked
      && !this.isSaving
      && !this.isCalculating;
  }

  get editSubCategoryOptions(): MultiSelectOption[] {
    const cityId = this.editForm.get('cityId')?.value as number | null;
    const list = cityId
      ? this.editSubCategories.filter(sc => sc.cityId === cityId && sc.isActive)
      : this.editSubCategories.filter(sc => sc.isActive);

    return list.map(sc => ({
      value: sc.subCategoryId,
      label: `${sc.name} · ${sc.price.toFixed(2)} ${this.localeService.translate('common.currency')}`
    }));
  }

  get canEditSubmit(): boolean {
    return this.editForm.valid && !this.isSaving && !this.selectedCustomerCashBlocked;
  }

  get calendarTitle(): string {
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    return this.calendarMonth.toLocaleDateString(locale, { month: 'long', year: 'numeric' });
  }

  get weekdayLabels(): string[] {
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    const base = new Date(2024, 0, 7); // Sunday
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(base);
      d.setDate(base.getDate() + i);
      return d.toLocaleDateString(locale, { weekday: 'short' });
    });
  }

  get nextCalendarTitle(): string {
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    return this.addMonths(this.calendarMonth, 1).toLocaleDateString(locale, {
      month: 'long',
      year: 'numeric'
    });
  }

  get availableVehiclesList(): AdminAvailableVehicleItemDto[] {
    return this.fleetVehicles.filter(v => v.isAvailable);
  }

  get unavailableVehiclesList(): AdminAvailableVehicleItemDto[] {
    return this.fleetVehicles.filter(v => !v.isAvailable);
  }

  trackByCalendarDay(_: number, day: CalendarDay): number {
    return day.date.getTime();
  }

  trackByCategoryId(_: number, item: CategoryDto): number {
    return item.categoryId;
  }

  trackBySubCategoryId(_: number, item: SubCategoryDto): number {
    return item.subCategoryId;
  }

  trackByVehicleId(_: number, item: AdminAvailableVehicleItemDto): number {
    return item.vehicleId;
  }

  trackByCustomerId(_: number, item: CustomerLookupDto): number {
    return item.customerId;
  }

  searchCustomerByPhone(): void {
    const mobile = this.customerPhoneQuery.trim().replace(/\s+/g, '');
    this.customerSearchError = '';
    this.customerSearchAttempted = true;

    if (this.countPhoneDigits(mobile) < 4) {
      this.customerSearchError = this.localeService.translate('orders.customerPhoneMinDigits');
      this.customerSearchResults = [];
      return;
    }

    this.isSearchingCustomer = true;
    this.customerClient.searchByMobile(mobile).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isSearchingCustomer = false; })
    ).subscribe({
      next: (results) => {
        this.customerSearchResults = results || [];
        if (this.customerSearchResults.length === 1) {
          this.selectCustomer(this.customerSearchResults[0]);
        }
      },
      error: (error) => {
        this.customerSearchResults = [];
        this.customerSearchError = this.extractErrorMessage(error)
          || this.localeService.translate('orders.customerSearchFailed');
      }
    });
  }

  selectCustomer(customer: CustomerLookupDto): void {
    this.selectedCustomer = customer;
    this.customerSearchResults = [];
    this.customerSearchError = '';
    this.customerPhoneQuery = customer.mobileNumber;

    const target = this.isEditMode ? this.editForm : this.orderForm;
    target.patchValue({ customerId: customer.customerId });
    target.get('customerId')?.markAsTouched();

    if (!this.isEditMode && customer.cityId) {
      this.cityAutoFilled = true;
      target.patchValue({ cityId: customer.cityId });
      target.get('cityId')?.markAsTouched();
    }
  }

  clearSelectedCustomer(): void {
    this.selectedCustomer = null;
    this.customerSearchResults = [];
    this.customerSearchError = '';
    this.customerPhoneQuery = '';
    this.customerSearchAttempted = false;
    this.cityAutoFilled = false;

    const target = this.isEditMode ? this.editForm : this.orderForm;
    target.patchValue({ customerId: null });
  }

  customerStateLabel(state: CustomerState): string {
    switch (state) {
      case CustomerState.Active:
        return this.localeService.translate('common.active');
      case CustomerState.Blocked:
        return this.localeService.translate('common.blocked');
      case CustomerState.InActive:
      default:
        return this.localeService.translate('common.inactive');
    }
  }

  unavailableReasonLabel(reason: string | null | undefined): string {
    if (reason === 'UnderMaintenance') {
      return this.localeService.translate('orders.reasonUnderMaintenance');
    }
    if (reason === 'Reserved') {
      return this.localeService.translate('orders.reasonReserved');
    }
    return this.localeService.translate('orders.reasonUnavailable');
  }

  hasBookedDays(vehicle: AdminAvailableVehicleItemDto): boolean {
    return vehicle.unavailableReason === 'Reserved'
      && Array.isArray(vehicle.conflictingDates)
      && vehicle.conflictingDates.length > 0;
  }

  openBookedDaysCalendar(vehicle: AdminAvailableVehicleItemDto, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    if (!this.hasBookedDays(vehicle)) return;

    this.bookedDaysVehicle = vehicle;
    const dates = this.getSortedBookedDates(vehicle);
    this.bookedCalendarMonth = this.startOfMonth(dates[0]);
  }

  closeBookedDaysCalendar(): void {
    this.bookedDaysVehicle = null;
  }

  get bookedDaysCount(): number {
    return this.bookedDaysVehicle ? this.getSortedBookedDates(this.bookedDaysVehicle).length : 0;
  }

  get bookedMonthTitle(): string {
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    return this.bookedCalendarMonth.toLocaleDateString(locale, { month: 'long', year: 'numeric' });
  }

  get bookedMonthsLabel(): string {
    if (!this.bookedDaysVehicle) return '';
    const locale = this.localeService.locale() === 'ar' ? 'ar-EG' : 'en-US';
    const labels = this.getBookedMonthStarts(this.bookedDaysVehicle).map(month =>
      month.toLocaleDateString(locale, { month: 'long', year: 'numeric' })
    );
    return labels.join(this.localeService.locale() === 'ar' ? ' · ' : ' · ');
  }

  get bookedCalendarDays(): BookedCalendarDay[] {
    if (!this.bookedDaysVehicle) return [];
    const bookedKeys = new Set(
      this.getSortedBookedDates(this.bookedDaysVehicle).map(d => this.dateKey(d))
    );

    const year = this.bookedCalendarMonth.getFullYear();
    const month = this.bookedCalendarMonth.getMonth();
    const first = new Date(year, month, 1);
    const startPad = first.getDay(); // Sunday = 0
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const cells: BookedCalendarDay[] = [];

    for (let i = 0; i < startPad; i++) {
      const d = new Date(year, month, i - startPad + 1);
      cells.push({ date: d, day: d.getDate(), inMonth: false, isBooked: false });
    }

    for (let day = 1; day <= daysInMonth; day++) {
      const d = new Date(year, month, day);
      cells.push({
        date: d,
        day,
        inMonth: true,
        isBooked: bookedKeys.has(this.dateKey(d))
      });
    }

    while (cells.length % 7 !== 0) {
      const last = cells[cells.length - 1].date;
      const d = new Date(last.getFullYear(), last.getMonth(), last.getDate() + 1);
      cells.push({ date: d, day: d.getDate(), inMonth: false, isBooked: false });
    }

    return cells;
  }

  canShiftBookedMonth(delta: number): boolean {
    if (!this.bookedDaysVehicle) return false;
    return this.findAdjacentBookedMonth(delta) !== null;
  }

  shiftBookedMonth(delta: number): void {
    const next = this.findAdjacentBookedMonth(delta);
    if (!next) return;
    this.bookedCalendarMonth = next;
  }

  private findAdjacentBookedMonth(delta: number): Date | null {
    if (!this.bookedDaysVehicle) return null;
    const months = this.getBookedMonthStarts(this.bookedDaysVehicle);
    const current = this.bookedCalendarMonth.getTime();
    const index = months.findIndex(m => m.getTime() === current);
    if (index < 0) return null;
    return months[index + delta] ?? null;
  }

  private getSortedBookedDates(vehicle: AdminAvailableVehicleItemDto): Date[] {
    return (vehicle.conflictingDates || [])
      .map(d => this.stripTime(new Date(d)))
      .filter(d => !Number.isNaN(d.getTime()))
      .sort((a, b) => a.getTime() - b.getTime());
  }

  private getBookedMonthStarts(vehicle: AdminAvailableVehicleItemDto): Date[] {
    const seen = new Set<string>();
    const months: Date[] = [];
    for (const date of this.getSortedBookedDates(vehicle)) {
      const start = this.startOfMonth(date);
      const key = this.dateKey(start);
      if (seen.has(key)) continue;
      seen.add(key);
      months.push(start);
    }
    return months;
  }

  private dateKey(date: Date): string {
    return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
  }

  onCancel(): void {
    if (this.isEditMode && this.orderId) {
      this.router.navigate(['/main/orders', this.orderId]);
      return;
    }
    this.router.navigate(['/main/orders']);
  }

  onPassportSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.errorMessage = this.localeService.translate('orders.passportInvalidType');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.passportPreview = reader.result as string;
      this.passportDirty = true;
      this.errorMessage = '';
    };
    reader.readAsDataURL(file);
  }

  clearPassport(): void {
    this.passportPreview = this.isEditMode ? this.existingPassportImage : null;
    this.passportDirty = false;
    if (this.passportInputRef?.nativeElement) {
      this.passportInputRef.nativeElement.value = '';
    }
  }

  isVehicleSelected(vehicleId: number): boolean {
    return this.selectedVehicles.some(v => v.vehicleId === vehicleId);
  }

  toggleVehicle(vehicle: AdminAvailableVehicleItemDto): void {
    if (!vehicle.isAvailable) return;

    const idx = this.selectedVehicles.findIndex(v => v.vehicleId === vehicle.vehicleId);
    if (idx >= 0) {
      this.selectedVehicles = this.selectedVehicles.filter(v => v.vehicleId !== vehicle.vehicleId);
    } else {
      this.selectedVehicles = [...this.selectedVehicles, vehicle];
    }
  }

  selectCategory(category: CategoryDto): void {
    this.selectedCategory = category;
    this.selectedSubCategory = null;
    this.clearFleetSelection(true);
    this.browseStep = 'subcategories';
    this.loadSubCategoriesForCategory(category.categoryId);
  }

  selectSubCategory(sub: SubCategoryDto): void {
    if (this.selectedSubCategory?.subCategoryId !== sub.subCategoryId) {
      this.clearFleetSelection(false);
    }
    this.selectedSubCategory = sub;
    this.browseStep = 'vehicles';
    if (this.hasCompleteRange) {
      this.loadAvailableVehicles();
    }
  }

  goToCategories(): void {
    this.browseStep = 'categories';
  }

  goToSubCategories(): void {
    if (this.selectedCategory) {
      this.browseStep = 'subcategories';
    }
  }

  shiftCalendar(delta: number): void {
    this.calendarMonth = this.addMonths(this.calendarMonth, delta);
    this.refreshCalendar();
  }

  onCalendarDayClick(day: CalendarDay, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    if (!day.inMonth || day.disabled) return;

    const clicked = this.stripTime(day.date);

    if (!this.rangeAnchor) {
      this.rangeAnchor = clicked;
      this.rangeFrom = clicked;
      this.rangeTo = null;
      this.hoverDate = null;
      this.syncRangeToForm();
      this.selectedVehicles = [];
      this.fleetVehicles = [];
      this.availableFleet = null;
      this.refreshCalendar();
      return;
    }

    let from = this.rangeAnchor;
    let to = clicked;

    if (to < from) {
      const tmp = from;
      from = to;
      to = tmp;
    }

    this.rangeAnchor = null;
    this.hoverDate = null;
    this.rangeFrom = from;
    this.rangeTo = to;
    this.syncRangeToForm();
    this.selectedVehicles = [];
    this.fleetVehicles = [];
    this.availableFleet = null;
    this.refreshCalendar();

    if (this.selectedSubCategory) {
      this.loadAvailableVehicles();
    }
  }

  onCalendarDayHover(day: CalendarDay): void {
    if (!this.rangeAnchor || !day.inMonth || day.disabled) {
      if (this.hoverDate) {
        this.hoverDate = null;
        this.refreshCalendar();
      }
      return;
    }
    const next = this.stripTime(day.date);
    if (this.hoverDate?.getTime() === next.getTime()) return;
    this.hoverDate = next;
    this.refreshCalendar();
  }

  onCalendarLeave(): void {
    if (!this.hoverDate) return;
    this.hoverDate = null;
    this.refreshCalendar();
  }

  clearDateRange(): void {
    this.rangeAnchor = null;
    this.hoverDate = null;
    this.rangeFrom = null;
    this.rangeTo = null;
    this.syncRangeToForm();
    this.selectedVehicles = [];
    this.fleetVehicles = [];
    this.availableFleet = null;
    this.refreshCalendar();
  }

  onReviewOrder(): void {
    this.orderForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.orderForm.invalid) return;

    const from = this.parseDateInput(this.orderForm.value.reservationDateFrom);
    const to = this.parseDateInput(this.orderForm.value.reservationDateTo);
    if (!from || !to || from > to) {
      this.errorMessage = this.localeService.translate('orders.invalidDateRange');
      return;
    }

    if (!this.passportDirty) {
      this.errorMessage = this.localeService.translate('orders.passportRequired');
      return;
    }

    if (!this.selectedSubCategory) {
      this.errorMessage = this.localeService.translate('orders.selectSubCategoryRequired');
      return;
    }

    if (this.selectedVehicleCount === 0) {
      this.errorMessage = this.localeService.translate('orders.selectVehiclesRequired');
      return;
    }

    if (this.selectedCustomerCashBlocked) return;

    const query = new AdminCalculateOrderTotalsQuery();
    query.customerId = this.orderForm.value.customerId;
    query.subCategoryId = this.selectedSubCategory.subCategoryId;
    query.cityId = this.orderForm.value.cityId;
    query.reservationDateFrom = from;
    query.reservationDateTo = to;
    query.isUrgent = !!this.orderForm.value.isUrgent;
    query.vehicleIds = this.selectedVehicleIds;

    this.isCalculating = true;
    this.orderClient.calculateTotals(query).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isCalculating = false; })
    ).subscribe({
      next: (preview) => {
        this.previewData = preview;
        this.modalError = '';
        this.isPreviewOpen = true;
      },
      error: (error) => {
        this.errorMessage = this.extractErrorMessage(error)
          || this.localeService.translate('orders.calculateFailed');
      }
    });
  }

  onClosePreview(): void {
    if (this.isSaving) return;
    this.isPreviewOpen = false;
    this.previewData = null;
    this.modalError = '';
  }

  onConfirmCreate(): void {
    if (!this.previewData || !this.selectedSubCategory || this.isSaving) return;

    const from = this.parseDateInput(this.orderForm.value.reservationDateFrom);
    const to = this.parseDateInput(this.orderForm.value.reservationDateTo);
    if (!from || !to) return;

    const command = new AdminCreateOrderCommand();
    command.customerId = this.orderForm.value.customerId;
    command.subCategoryId = this.selectedSubCategory.subCategoryId;
    command.cityId = this.orderForm.value.cityId;
    command.reservationDateFrom = from;
    command.reservationDateTo = to;
    command.vehicleIds = this.selectedVehicleIds;
    command.notes = this.orderForm.value.notes?.trim() || null;
    command.passportImage = this.passportPreview || '';
    command.hotelName = this.orderForm.value.hotelName?.trim();
    command.hotelAddress = this.orderForm.value.hotelAddress?.trim();
    command.hotelPhone = this.orderForm.value.hotelPhone?.trim() || null;
    command.isUrgent = !!this.orderForm.value.isUrgent;

    this.isSaving = true;
    this.modalError = '';

    this.orderClient.createOrder(command).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isSaving = false; })
    ).subscribe({
      next: (order) => {
        this.isPreviewOpen = false;
        this.router.navigate(['/main/orders', order.orderId]);
      },
      error: (error) => {
        this.modalError = this.extractErrorMessage(error)
          || this.localeService.translate('orders.failedToCreate');
      }
    });
  }

  onSubmitEdit(): void {
    if (!this.canEditSubmit || !this.orderId) {
      this.editForm.markAllAsTouched();
      return;
    }

    const from = this.parseDateInput(this.editForm.value.reservationDateFrom);
    const to = this.parseDateInput(this.editForm.value.reservationDateTo);
    if (!from || !to || from > to) {
      this.errorMessage = this.localeService.translate('orders.invalidDateRange');
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const command = new AdminUpdateOrderCommand();
    command.orderId = this.orderId;
    command.customerId = this.editForm.value.customerId;
    command.cityId = this.editForm.value.cityId;
    command.subCategoryId = this.editForm.value.subCategoryId;
    command.reservationDateFrom = from;
    command.reservationDateTo = to;
    command.vehiclesCount = +this.editForm.value.vehiclesCount;
    command.hotelName = this.editForm.value.hotelName?.trim();
    command.hotelAddress = this.editForm.value.hotelAddress?.trim();
    command.hotelPhone = this.editForm.value.hotelPhone?.trim() || null;
    command.notes = this.editForm.value.notes?.trim() || null;
    command.isUrgent = !!this.editForm.value.isUrgent;
    command.paymentMethodId = PaymentMethod.Cash;
    command.passportImage = this.passportDirty ? (this.passportPreview || '') : null;

    this.orderClient.updateOrder(this.orderId, command).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isSaving = false; })
    ).subscribe({
      next: () => this.router.navigate(['/main/orders', this.orderId]),
      error: (error) => {
        this.errorMessage = this.extractErrorMessage(error)
          || this.localeService.translate('orders.failedToUpdate');
      }
    });
  }

  private clearFleetSelection(resetDates: boolean): void {
    this.closeBookedDaysCalendar();
    this.fleetVehicles = [];
    this.availableFleet = null;
    this.selectedVehicles = [];
    if (resetDates) {
      this.rangeAnchor = null;
      this.hoverDate = null;
      this.rangeFrom = null;
      this.rangeTo = null;
      this.syncRangeToForm();
      this.refreshCalendar();
    }
  }

  private syncRangeToForm(): void {
    this.orderForm.patchValue({
      reservationDateFrom: this.rangeFrom ? this.formatDateInput(this.rangeFrom) : '',
      reservationDateTo: this.rangeTo ? this.formatDateInput(this.rangeTo) : ''
    }, { emitEvent: false });
  }

  private refreshCalendar(): void {
    this.calendarDays = this.buildCalendarDays(this.calendarMonth);
    this.nextCalendarDays = this.buildCalendarDays(this.addMonths(this.calendarMonth, 1));
  }

  private resetBrowseState(): void {
    this.browseStep = 'categories';
    this.categories = [];
    this.selectedCategory = null;
    this.subCategories = [];
    this.selectedSubCategory = null;
    this.clearFleetSelection(true);
  }

  private loadCategoriesForCity(cityId: number): void {
    this.isLoadingCategories = true;
    this.categories = [];
    this.categoryClient.getAll(1, 100, undefined, [cityId], true).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isLoadingCategories = false; })
    ).subscribe({
      next: (result) => { this.categories = result.items || []; },
      error: () => { this.errorMessage = this.localeService.translate('common.failedToLoad'); }
    });
  }

  private loadSubCategoriesForCategory(categoryId: number): void {
    const cityId = this.orderForm.get('cityId')?.value as number | null;
    this.isLoadingSubCategories = true;
    this.subCategories = [];
    this.subCategoryClient.getAll(1, 100, undefined, categoryId, undefined, cityId ? [cityId] : undefined, true).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isLoadingSubCategories = false; })
    ).subscribe({
      next: (result) => { this.subCategories = result.items || []; },
      error: () => { this.errorMessage = this.localeService.translate('common.failedToLoad'); }
    });
  }

  private loadAvailableVehicles(): void {
    const cityId = this.orderForm.get('cityId')?.value as number | null;
    const subId = this.selectedSubCategory?.subCategoryId;
    // Use component range state (not form) so same-day from===to is never lost.
    const from = this.rangeFrom;
    const to = this.rangeTo;

    if (!cityId || !subId || !from || !to || from > to) {
      this.fleetVehicles = [];
      this.availableFleet = null;
      return;
    }

    this.syncRangeToForm();
    this.isLoadingVehicles = true;
    this.fleetVehicles = [];
    this.selectedVehicles = [];

    this.orderClient.getAvailableVehicles(subId, cityId, from, to).pipe(
      takeUntil(this.destroy$),
      finalize(() => { this.isLoadingVehicles = false; })
    ).subscribe({
      next: (result) => {
        this.availableFleet = result;
        this.fleetVehicles = result.vehicles || [];
      },
      error: (error) => {
        this.availableFleet = null;
        this.fleetVehicles = [];
        this.errorMessage = this.extractErrorMessage(error)
          || this.localeService.translate('orders.vehiclesLoadFailed');
      }
    });
  }

  private buildCalendarDays(month: Date): CalendarDay[] {
    const year = month.getFullYear();
    const monthIndex = month.getMonth();
    const first = new Date(year, monthIndex, 1);
    const startPad = first.getDay(); // 0 Sun
    const daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
    const today = this.stripTime(new Date());

    let previewFrom = this.rangeFrom;
    let previewTo = this.rangeTo;

    if (this.rangeAnchor && this.hoverDate) {
      previewFrom = this.rangeAnchor;
      previewTo = this.hoverDate;
      if (previewTo < previewFrom) {
        const tmp = previewFrom;
        previewFrom = previewTo;
        previewTo = tmp;
      }
    } else if (this.rangeAnchor) {
      previewFrom = this.rangeAnchor;
      previewTo = null;
    }

    const cells: CalendarDay[] = [];
    const totalCells = Math.ceil((startPad + daysInMonth) / 7) * 7;

    for (let i = 0; i < totalCells; i++) {
      const dayNum = i - startPad + 1;
      const inMonth = dayNum >= 1 && dayNum <= daysInMonth;
      const date = new Date(year, monthIndex, inMonth ? dayNum : 1);
      const stripped = this.stripTime(date);
      const disabled = !inMonth || stripped < today;

      const isStart = !!(previewFrom && inMonth && stripped.getTime() === previewFrom.getTime());
      const isEnd = !!(previewTo && inMonth && stripped.getTime() === previewTo.getTime());
      const inRange = !!(
        inMonth
        && previewFrom
        && previewTo
        && stripped > previewFrom
        && stripped < previewTo
      );

      cells.push({
        date: stripped,
        day: inMonth ? dayNum : 0,
        inMonth,
        disabled,
        isToday: inMonth && stripped.getTime() === today.getTime(),
        isStart,
        isEnd,
        inRange
      });
    }

    return cells;
  }

  private loadOrder(id: number): void {
    this.orderClient.getOrderById(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (order: OrderDetailDto) => {
        if (
          order.orderState !== OrderState.Pending
          || order.orderCancellationFee
          || order.moneyRefunded
          || order.refundablePaypalAmount
        ) {
          this.errorMessage = this.localeService.translate('orders.editNotAllowed');
          this.isBootstrapping = false;
          setTimeout(() => this.router.navigate(['/main/orders', id]), 1200);
          return;
        }

        const paidOrRefunded = order.orderPayments?.some(
          p => p.state === PaymentState.Paid || p.state === PaymentState.Refunded
        );
        if (paidOrRefunded) {
          this.errorMessage = this.localeService.translate('orders.editNotAllowed');
          this.isBootstrapping = false;
          setTimeout(() => this.router.navigate(['/main/orders', id]), 1200);
          return;
        }

        this.orderCode = order.orderCode;
        this.existingPassportImage = order.passportImage || null;
        this.passportPreview = order.passportImage || null;
        this.ensureSubCategoryOption(order);
        this.applyOrderCustomer(order);

        this.editForm.patchValue({
          customerId: order.customerId,
          cityId: order.cityId,
          subCategoryId: order.subCategoryId,
          reservationDateFrom: this.formatDateInput(order.reservationDateFrom),
          reservationDateTo: this.formatDateInput(order.reservationDateTo),
          vehiclesCount: order.vehiclesCount,
          hotelName: order.hotelName,
          hotelAddress: order.hotelAddress,
          hotelPhone: order.hotelPhone || '',
          notes: order.notes || '',
          isUrgent: order.isUrgent
        }, { emitEvent: false });

        this.isBootstrapping = false;
      },
      error: () => {
        this.errorMessage = this.localeService.translate('orders.loadFailed');
        this.isBootstrapping = false;
      }
    });
  }

  private applyOrderCustomer(order: OrderDetailDto): void {
    const lookup = new CustomerLookupDto();
    lookup.customerId = order.customerId;
    lookup.fullName = order.customerName;
    lookup.mobileNumber = order.customerMobileNumber || '';
    lookup.cityId = order.cityId;
    lookup.cityName = order.cityName;
    lookup.cashBlock = false;
    lookup.state = CustomerState.Active;
    this.selectedCustomer = lookup;
    this.customerPhoneQuery = lookup.mobileNumber;
    this.customerSearchResults = [];
    this.customerSearchAttempted = false;
  }

  private ensureSubCategoryOption(order: OrderDetailDto): void {
    if (this.editSubCategories.some(sc => sc.subCategoryId === order.subCategoryId)) return;
    const stub = new SubCategoryDto();
    stub.subCategoryId = order.subCategoryId;
    stub.name = order.subCategoryName;
    stub.price = order.subCategoryPrice;
    stub.cityId = order.cityId;
    stub.cityName = order.cityName;
    stub.isActive = true;
    stub.isOffer = false;
    stub.description = '';
    stub.categoryId = 0;
    stub.categoryName = '';
    stub.vehicleCount = 0;
    this.editSubCategories = [stub, ...this.editSubCategories];
  }

  private bindFormListeners(): void {
    this.orderForm.get('cityId')?.valueChanges.pipe(
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe((cityId: number | null) => {
      if (
        this.selectedCustomer?.cityId
        && cityId
        && cityId !== this.selectedCustomer.cityId
      ) {
        this.cityAutoFilled = false;
      }
      this.resetBrowseState();
      if (cityId) this.loadCategoriesForCity(cityId);
    });

    this.editForm.get('cityId')?.valueChanges.pipe(
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe((cityId: number | null) => {
      if (!cityId) return;
      const subId = this.editForm.get('subCategoryId')?.value as number | null;
      if (subId) {
        const stillValid = this.editSubCategories.some(
          sc => sc.subCategoryId === subId && sc.cityId === cityId
        );
        if (!stillValid) {
          this.editForm.patchValue({ subCategoryId: null }, { emitEvent: false });
        }
      }
    });
  }

  private loadLookups(): void {
    const subCategories$ = this.isEditMode
      ? this.subCategoryClient.getAll(1, 1000, undefined, undefined, undefined, undefined, true)
      : of(null);

    forkJoin({
      cities: this.cityClient.getAll(1, 500, undefined, true),
      subCategories: subCategories$
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ cities, subCategories }) => {
        this.cities = cities.items || [];
        if (subCategories) {
          this.editSubCategories = subCategories.items || [];
        }

        if (this.isEditMode && this.orderId) {
          this.loadOrder(this.orderId);
        } else {
          this.isBootstrapping = false;
        }
      },
      error: () => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isBootstrapping = false;
      }
    });
  }

  private countPhoneDigits(value: string): number {
    let count = 0;
    for (const ch of value || '') {
      if (ch >= '0' && ch <= '9') count++;
    }
    return count;
  }

  private stripTime(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 12, 0, 0);
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1, 12, 0, 0);
  }

  private addMonths(date: Date, delta: number): Date {
    return new Date(date.getFullYear(), date.getMonth() + delta, 1, 12, 0, 0);
  }

  private formatDateInput(date: Date | string | null | undefined): string {
    if (!date) return '';
    const d = date instanceof Date ? date : new Date(date);
    if (Number.isNaN(d.getTime())) return '';
    const y = d.getFullYear();
    const m = `${d.getMonth() + 1}`.padStart(2, '0');
    const day = `${d.getDate()}`.padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private parseDateInput(value: string | null | undefined): Date | null {
    if (!value) return null;
    const [y, m, d] = value.split('-').map(Number);
    if (!y || !m || !d) return null;
    return new Date(y, m - 1, d, 12, 0, 0);
  }

  private extractErrorMessage(error: any): string {
    if (!error) return '';
    if (typeof error === 'string') return error;
    return error.errorMessage
      || error.detail
      || error.title
      || error.result?.errorMessage
      || error.result?.detail
      || error.error?.errorMessage
      || error.error?.detail
      || error.message
      || '';
  }
}
