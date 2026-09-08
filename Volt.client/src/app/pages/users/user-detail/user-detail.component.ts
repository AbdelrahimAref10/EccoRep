import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminUserClient, UserDto, UpdateUserCommand, RoleClient, RoleDto } from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  MultiSelectComponent,
  MultiSelectOption
} from '../../../shared/components/multi-select/multi-select.component';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, MultiSelectComponent, TranslatePipe],
  templateUrl: './user-detail.component.html',
  styleUrls: ['./user-detail.component.css', '../../../shared/styles/entity-form.css']
})
export class UserDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminUserClient = inject(AdminUserClient);
  private roleClient = inject(RoleClient);
  private fb = inject(FormBuilder);
  private localeService = inject(LocaleService);

  user: UserDto | null = null;
  userId = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  isEditing = false;
  actionLoading = '';

  userForm: FormGroup;
  availableRoles: RoleDto[] = [];
  isLoadingRoles = false;
  showPassword = false;

  get roleOptions(): MultiSelectOption[] {
    return this.availableRoles
      .filter(role => !!role.roleName)
      .map(role => ({
        value: role.roleName!,
        label: role.roleName || this.localeService.translate('users.unnamedRole')
      }));
  }

  get isUserActive(): boolean {
    if (!this.user) return false;
    return this.user.isActive ?? this.user.active;
  }

  constructor() {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required]],
      password: [''],
      role: [null, [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.userId = +params['id'];
      if (this.userId) {
        this.loadUser();
        this.loadRoles();
      }
    });

    this.route.queryParams.subscribe(query => {
      if (query['edit'] === '1' || query['edit'] === 'true') {
        this.isEditing = true;
      }
    });
  }

  loadUser(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.adminUserClient.getById(this.userId).subscribe({
      next: (user: UserDto) => {
        this.user = user;
        this.userForm.patchValue({
          userName: user.userName,
          email: user.email || '',
          phoneNumber: user.phoneNumber || '',
          password: '',
          role: user.roles && user.roles.length > 0 ? user.roles[0] : null
        });
        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('users.loadFailed');
        this.isLoading = false;
        console.error('Error loading user:', error);
      }
    });
  }

  loadRoles(): void {
    this.isLoadingRoles = true;
    this.roleClient.getAllRoles().subscribe({
      next: (roles: RoleDto[]) => {
        this.availableRoles = roles.filter(r => r.roleName && r.roleName.length > 0);
        this.isLoadingRoles = false;
      },
      error: (error: unknown) => {
        console.error('Error loading roles:', error);
        this.isLoadingRoles = false;
      }
    });
  }

  getRoleIdByName(roleName: string): number | null {
    const role = this.availableRoles.find(r => r.roleName === roleName);
    return role ? role.roleId : null;
  }

  onEdit(): void {
    this.isEditing = true;
    this.showPassword = false;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
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

  onCancel(): void {
    this.isEditing = false;
    this.showPassword = false;
    if (this.user) {
      this.userForm.patchValue({
        userName: this.user.userName,
        email: this.user.email || '',
        phoneNumber: this.user.phoneNumber || '',
        password: '',
        role: this.user.roles && this.user.roles.length > 0 ? this.user.roles[0] : null
      });
    }
  }

  onSave(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.actionLoading = 'save';
    const formValue = this.userForm.value;
    const command = new UpdateUserCommand();
    command.userId = this.userId;
    command.userName = formValue.userName;
    command.email = formValue.email || null;
    command.phoneNumber = formValue.phoneNumber || null;
    command.password = formValue.password && formValue.password.trim() !== '' ? formValue.password : null;

    const roleId = this.getRoleIdByName(formValue.role);
    if (!roleId) {
      this.showErrorMessage(this.localeService.translate('users.invalidRole'));
      this.actionLoading = '';
      return;
    }
    command.roleId = roleId;

    this.adminUserClient.update(this.userId, command).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('users.updatedSuccess'));
        this.isEditing = false;
        this.loadUser();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(error.error?.detail || error.error?.title || this.localeService.translate('users.updateFailed'));
        this.actionLoading = '';
      }
    });
  }

  onDelete(): void {
    if (!confirm(this.localeService.translate('users.deleteConfirm'))) {
      return;
    }

    this.actionLoading = 'delete';
    this.adminUserClient.delete(this.userId).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('users.deletedSuccess'));
        setTimeout(() => {
          this.router.navigate(['/main/users']);
        }, 1500);
      },
      error: (error: any) => {
        this.showErrorMessage(error.error?.detail || error.error?.title || this.localeService.translate('users.deleteFailed'));
        this.actionLoading = '';
      }
    });
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

  onBack(): void {
    this.router.navigate(['/main/users']);
  }

  isLockedOut(): boolean {
    if (!this.user || !this.user.lockoutEnd) return false;
    return new Date(this.user.lockoutEnd) > new Date();
  }

  getLockoutStatus(): string {
    return this.isUserActive
      ? this.localeService.translate('common.active')
      : this.localeService.translate('common.inactive');
  }

  onActivate(): void {
    if (!confirm(this.localeService.translate('users.activateConfirm'))) {
      return;
    }

    this.actionLoading = 'activate';
    this.adminUserClient.activate(this.userId).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('users.activatedSuccess'));
        this.loadUser();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(error.error?.detail || error.error?.title || this.localeService.translate('users.activateFailed'));
        this.actionLoading = '';
      }
    });
  }

  onDeactivate(): void {
    if (!confirm(this.localeService.translate('users.deactivateConfirm'))) {
      return;
    }

    this.actionLoading = 'deactivate';
    this.adminUserClient.deactivate(this.userId).subscribe({
      next: () => {
        this.showSuccessMessage(this.localeService.translate('users.deactivatedSuccess'));
        this.loadUser();
        this.actionLoading = '';
      },
      error: (error: any) => {
        this.showErrorMessage(error.error?.detail || error.error?.title || this.localeService.translate('users.deactivateFailed'));
        this.actionLoading = '';
      }
    });
  }
}
