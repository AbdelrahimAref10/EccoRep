import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { AdminUserClient, UserDto, PagedResultOfUserDto, CreateUserCommand, RoleClient, RoleDto } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { AppRole, AppRoleNames, appRoleFromName } from '../../core/models/app-role';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    TranslatePipe,
    MultiSelectComponent,
    PaginationComponent
  ],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css', '../../shared/styles/list-filters.css', '../../shared/styles/entity-form.css']
})
export class UsersComponent implements OnInit, OnDestroy {
  private adminUserClient = inject(AdminUserClient);
  private roleClient = inject(RoleClient);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private localeService = inject(LocaleService);
  private destroy$ = new Subject<void>();

  /** Roles creatable from admin dashboard (Customer uses admin customers flow). */
  private readonly creatableRoleNames = [
    AppRoleNames.SuperAdmin,
    AppRoleNames.Merchant,
    AppRoleNames.Delivery
  ];

  users: UserDto[] = [];
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  searchTerm = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  selectedRoleValues: Array<string | number | boolean> = [];
  selectedStatusValues: Array<string | number | boolean> = [];

  showModal = false;
  userForm: FormGroup;
  roles: RoleDto[] = [];
  availableRoles: string[] = [];
  isLoadingRoles = false;
  isSubmitting = false;
  showPassword = false;

  currentStep = 1;
  totalSteps = 3;
  steps = [
    { number: 1, titleKey: 'users.stepBasic', fields: ['userName', 'fullName', 'email', 'phoneNumber'] },
    { number: 2, titleKey: 'users.stepSecurity', fields: ['password'] },
    { number: 3, titleKey: 'users.stepRoles', fields: ['role'] }
  ];

  get roleFilterOptions(): MultiSelectOption[] {
    return this.availableRoles.map(role => ({
      value: role,
      label: role
    }));
  }

  get activeFilterOptions(): MultiSelectOption[] {
    return [
      { value: 'true', label: this.localeService.translate('common.active') },
      { value: 'false', label: this.localeService.translate('common.inactive') }
    ];
  }

  get roleFormOptions(): MultiSelectOption[] {
    return this.creatableRoleNames
      .filter(name => this.availableRoles.includes(name))
      .map(role => ({
        value: role,
        label: role
      }));
  }

  get activeFilterCount(): number {
    let count = 0;
    if (this.searchTerm.trim()) count++;
    count += this.selectedRoleValues.length;
    count += this.selectedStatusValues.length;
    return count;
  }

  constructor() {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordValidator]],
      role: [null, [Validators.required]]
    });
  }

  passwordValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null;
    }

    const password = control.value;
    const hasLowercase = /[a-z]/.test(password);
    const hasUppercase = /[A-Z]/.test(password);
    const errors: ValidationErrors = {};

    if (!hasLowercase) {
      errors['passwordRequireLower'] = true;
    }
    if (!hasUppercase) {
      errors['passwordRequireUpper'] = true;
    }

    return Object.keys(errors).length > 0 ? errors : null;
  }

  getPasswordStrength(): { strength: 'weak' | 'medium' | 'strong'; percentage: number } {
    const password = this.userForm.get('password')?.value || '';
    if (!password) return { strength: 'weak', percentage: 0 };

    let strength = 0;
    if (password.length >= 8) strength += 25;
    if (password.length >= 12) strength += 10;
    if (/[a-z]/.test(password)) strength += 20;
    if (/[A-Z]/.test(password)) strength += 20;
    if (/[0-9]/.test(password)) strength += 15;
    if (/[^a-zA-Z0-9]/.test(password)) strength += 10;

    if (strength < 50) return { strength: 'weak', percentage: strength };
    if (strength < 80) return { strength: 'medium', percentage: strength };
    return { strength: 'strong', percentage: strength };
  }

  hasFieldError(fieldName: string): boolean {
    const field = this.userForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.userForm.get(fieldName);
    if (!field || !field.errors) return '';

    if (field.errors['required']) {
      return this.localeService.translate('users.fieldRequired', { field: this.getFieldLabel(fieldName) });
    }
    if (field.errors['minlength']) {
      const requiredLength = field.errors['minlength'].requiredLength;
      return this.localeService.translate('users.fieldMinLength', {
        field: this.getFieldLabel(fieldName),
        count: requiredLength
      });
    }
    if (field.errors['email']) {
      return this.localeService.translate('users.emailInvalid');
    }
    if (field.errors['passwordRequireLower']) {
      return this.localeService.translate('users.passwordRequireLower');
    }
    if (field.errors['passwordRequireUpper']) {
      return this.localeService.translate('users.passwordRequireUpper');
    }
    return '';
  }

  getFieldLabel(fieldName: string): string {
    const keys: Record<string, string> = {
      userName: 'users.userName',
      fullName: 'users.fullName',
      email: 'common.email',
      phoneNumber: 'common.phone',
      password: 'users.password',
      role: 'users.role'
    };
    return this.localeService.translate(keys[fieldName] || fieldName);
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  hasPasswordMinLength(): boolean {
    const password = this.userForm.get('password')?.value || '';
    return password.length >= 8;
  }

  hasPasswordLowercase(): boolean {
    const password = this.userForm.get('password')?.value || '';
    return /[a-z]/.test(password);
  }

  hasPasswordUppercase(): boolean {
    const password = this.userForm.get('password')?.value || '';
    return /[A-Z]/.test(password);
  }

  ngOnInit(): void {
    this.loadRoles();
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRoles(): void {
    this.isLoadingRoles = true;
    this.roleClient.getAllRoles().subscribe({
      next: (roles: RoleDto[]) => {
        this.roles = roles;
        this.availableRoles = roles.map(r => r.roleName || '').filter(name => name.length > 0);
        this.isLoadingRoles = false;
      },
      error: (error: unknown) => {
        console.error('Error loading roles:', error);
        this.isLoadingRoles = false;
      }
    });
  }

  loadUsers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const role = this.selectedRoleValues.length === 1 ? String(this.selectedRoleValues[0]) : undefined;
    const active = this.toNullableBool(this.selectedStatusValues);

    this.adminUserClient.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      role,
      active
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result: PagedResultOfUserDto) => {
        this.users = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading users:', error);
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadUsers();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedRoleValues = [];
    this.selectedStatusValues = [];
    this.applyFilters();
  }

  onRoleFilterChange(values: Array<string | number | boolean>): void {
    this.selectedRoleValues = values;
  }

  onStatusFilterChange(values: Array<string | number | boolean>): void {
    this.selectedStatusValues = values;
  }

  onAddNew(): void {
    this.userForm.reset();
    this.userForm.patchValue({ role: null });
    this.currentStep = 1;
    this.showPassword = false;
    this.errorMessage = '';
    this.showModal = true;
  }

  getFormProgress(): number {
    const totalFields = this.steps.reduce((sum, step) => sum + step.fields.length, 0);
    let completedFields = 0;

    this.steps.forEach(step => {
      step.fields.forEach(field => {
        const control = this.userForm.get(field);
        if (control && control.valid && control.value) {
          completedFields++;
        }
      });
    });

    return Math.round((completedFields / totalFields) * 100);
  }

  isStepValid(stepNumber: number): boolean {
    const step = this.steps.find(s => s.number === stepNumber);
    if (!step) return false;

    return step.fields.every(field => {
      const control = this.userForm.get(field);
      return !!(control && control.valid);
    });
  }

  canGoToNextStep(): boolean {
    return this.isStepValid(this.currentStep);
  }

  onNextStep(): void {
    if (this.canGoToNextStep() && this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  onPreviousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  goToStep(stepNumber: number): void {
    if (stepNumber >= 1 && stepNumber <= this.totalSteps) {
      if (stepNumber < this.currentStep || this.isStepValid(this.currentStep)) {
        this.currentStep = stepNumber;
      }
    }
  }

  isStep1(): boolean {
    return this.currentStep === 1;
  }

  isStep2(): boolean {
    return this.currentStep === 2;
  }

  isStep3(): boolean {
    return this.currentStep === 3;
  }

  onView(userId: number): void {
    this.router.navigate(['/main/users', userId]);
  }

  onEdit(userId: number): void {
    this.router.navigate(['/main/users', userId], { queryParams: { edit: '1' } });
  }

  onDelete(userId: number): void {
    if (confirm(this.localeService.translate('users.deleteConfirm'))) {
      this.adminUserClient.delete(userId).subscribe({
        next: () => {
          this.showSuccessMessage(this.localeService.translate('users.deletedSuccess'));
          this.loadUsers();
        },
        error: (error: any) => {
          const errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('users.deleteFailed');
          this.showErrorMessage(errorMessage);
          console.error('Error deleting user:', error);
        }
      });
    }
  }

  onActivate(userId: number): void {
    if (confirm(this.localeService.translate('users.activateConfirm'))) {
      this.isLoading = true;
      this.adminUserClient.activate(userId).subscribe({
        next: () => {
          this.isLoading = false;
          this.showSuccessMessage(this.localeService.translate('users.activatedSuccess'));
          this.loadUsers();
        },
        error: (error: any) => {
          this.isLoading = false;
          const errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('users.activateFailed');
          this.showErrorMessage(errorMessage);
          console.error('Error activating user:', error);
        }
      });
    }
  }

  onDeactivate(userId: number): void {
    if (confirm(this.localeService.translate('users.deactivateConfirm'))) {
      this.isLoading = true;
      this.adminUserClient.deactivate(userId).subscribe({
        next: () => {
          this.isLoading = false;
          this.showSuccessMessage(this.localeService.translate('users.deactivatedSuccess'));
          this.loadUsers();
        },
        error: (error: any) => {
          this.isLoading = false;
          const errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('users.deactivateFailed');
          this.showErrorMessage(errorMessage);
          console.error('Error deactivating user:', error);
        }
      });
    }
  }

  async onSubmit(): Promise<void> {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formValue = this.userForm.value;

    const role = appRoleFromName(formValue.role);
    if (role == null || role === AppRole.Customer) {
      this.showErrorMessage(this.localeService.translate('users.invalidRole'));
      this.isSubmitting = false;
      return;
    }

    const command = new CreateUserCommand();
    command.userName = formValue.userName;
    command.fullName = formValue.fullName;
    command.email = formValue.email || null;
    command.phoneNumber = formValue.phoneNumber || null;
    command.password = formValue.password;
    command.role = role;

    this.adminUserClient.create(command).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showModal = false;
        this.userForm.reset();
        this.userForm.patchValue({ role: null });
        this.errorMessage = '';
        this.showSuccessMessage(this.localeService.translate('users.createdSuccess'));
        this.loadUsers();
      },
      error: (error: any) => {
        const errorMessage = this.extractErrorMessage(error);
        this.showErrorMessage(errorMessage);
        this.isSubmitting = false;
        console.error('Error creating user:', error);
      }
    });
  }

  onCloseModal(): void {
    this.showModal = false;
    this.userForm.reset();
    this.userForm.patchValue({ role: null });
    this.currentStep = 1;
    this.isSubmitting = false;
    this.showPassword = false;
    this.errorMessage = '';
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= Math.max(this.totalPages, 1) && page !== this.currentPage) {
      this.currentPage = page;
      this.loadUsers();
      window.scrollTo({ top: 0, behavior: 'smooth' });
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

  extractErrorMessage(error: any): string {
    if (!error) {
      return this.localeService.translate('users.createFailed');
    }

    const errorObj = error.error || error;

    if (errorObj.errors && Array.isArray(errorObj.errors)) {
      return errorObj.errors.join(', ');
    }

    if (errorObj.errors && typeof errorObj.errors === 'object') {
      const errorMessages: string[] = [];
      for (const key in errorObj.errors) {
        if (Object.prototype.hasOwnProperty.call(errorObj.errors, key)) {
          const fieldErrors = errorObj.errors[key];
          if (Array.isArray(fieldErrors)) {
            errorMessages.push(...fieldErrors);
          } else if (typeof fieldErrors === 'string') {
            errorMessages.push(fieldErrors);
          }
        }
      }
      if (errorMessages.length > 0) {
        return errorMessages.join(', ');
      }
    }

    if (errorObj.detail) return errorObj.detail;
    if (errorObj.title) return errorObj.title;
    if (errorObj.message) return errorObj.message;
    if (typeof errorObj === 'string') return errorObj;
    if (error.statusText) return error.statusText;

    return this.localeService.translate('users.createFailed');
  }

  getRolesDisplay(roles: string[]): string {
    return roles && roles.length > 0
      ? roles.join(', ')
      : this.localeService.translate('users.noRoles');
  }

  private toNullableBool(values: Array<string | number | boolean>): boolean | undefined {
    if (values.length !== 1) {
      return undefined;
    }
    return String(values[0]) === 'true';
  }
}
