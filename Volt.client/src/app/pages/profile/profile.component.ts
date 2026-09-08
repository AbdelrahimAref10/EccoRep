import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminUserClient, UserDto, UpdateUserCommand, RoleClient, RoleDto } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, TranslatePipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private adminUserClient = inject(AdminUserClient);
  private roleClient = inject(RoleClient);
  private fb = inject(FormBuilder);
  private localeService = inject(LocaleService);

  user: UserDto | null = null;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  isEditing = false;
  actionLoading: string = '';

  userForm: FormGroup;
  availableRoles: RoleDto[] = [];
  isLoadingRoles = false;
  
  // Multi-step form
  currentStep: number = 1;
  totalSteps: number = 2;
  showPassword: boolean = false;
  steps = [
    { number: 1, title: 'Personal Information', fields: ['userName', 'email', 'phoneNumber'] },
    { number: 2, title: 'Security', fields: ['password'] }
  ];

  constructor() {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required]],
      password: ['']
    });
  }

  ngOnInit(): void {
    this.loadUser();
    this.loadRoles();
  }

  loadUser(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.adminUserClient.getCurrent().subscribe({
      next: (user: UserDto) => {
        this.user = user;
        this.userForm.patchValue({
          userName: user.userName,
          email: user.email || '',
          phoneNumber: user.phoneNumber || '',
          password: ''
        });
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading user profile:', error);
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
      error: (error: any) => {
        console.error('Error loading roles:', error);
        this.isLoadingRoles = false;
      }
    });
  }

  onEdit(): void {
    this.isEditing = true;
    this.currentStep = 1;
  }

  // Multi-step form methods
  getFormProgress(): number {
    const totalFields = this.steps.reduce((sum, step) => sum + step.fields.length, 0);
    let completedFields = 0;
    
    this.steps.forEach(step => {
      step.fields.forEach(field => {
        const control = this.userForm.get(field);
        if (control && control.valid && (control.value || field === 'password')) {
          completedFields++;
        }
      });
    });
    
    return Math.round((completedFields / totalFields) * 100);
  }

  getStepProgress(stepNumber: number): number {
    const step = this.steps.find(s => s.number === stepNumber);
    if (!step) return 0;
    
    let completedFields = 0;
    step.fields.forEach(field => {
      const control = this.userForm.get(field);
      if (control && control.valid && (control.value || field === 'password')) {
        completedFields++;
      }
    });
    
    return Math.round((completedFields / step.fields.length) * 100);
  }

  isStepValid(stepNumber: number): boolean {
    const step = this.steps.find(s => s.number === stepNumber);
    if (!step) return false;
    
    return step.fields.every(field => {
      const control = this.userForm.get(field);
      // Password is optional for update
      if (field === 'password') return true;
      return control && control.valid;
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

  onCancel(): void {
    this.isEditing = false;
    this.currentStep = 1;
    if (this.user) {
      this.userForm.patchValue({
        userName: this.user.userName,
        email: this.user.email || '',
        phoneNumber: this.user.phoneNumber || '',
        password: ''
      });
    }
  }

  onSave(): void {
    if (this.userForm.invalid || !this.user) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.actionLoading = 'save';
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.userForm.value;
    const command = new UpdateUserCommand();
    command.userId = this.user.id;
    command.userName = formValue.userName;
    command.email = formValue.email || null;
    command.phoneNumber = formValue.phoneNumber || null;
    command.password = formValue.password || null;
    
    // Get current role ID - users shouldn't change their role, so keep the existing one
    if (this.user.roles && this.user.roles.length > 0) {
      const currentRoleName = this.user.roles[0];
      const currentRole = this.availableRoles.find(r => r.roleName === currentRoleName);
      if (currentRole) {
        command.roleId = currentRole.roleId;
      } else {
        // If role not found, try to get first available role as fallback
        if (this.availableRoles.length > 0) {
          command.roleId = this.availableRoles[0].roleId;
        } else {
          this.showErrorMessage('Unable to determine user role. Please contact administrator.');
          this.actionLoading = '';
          return;
        }
      }
    } else {
      // If user has no role, we can't update - this shouldn't happen but handle it
      this.showErrorMessage('User has no assigned role. Please contact administrator.');
      this.actionLoading = '';
      return;
    }

    this.adminUserClient.update(this.user.id, command).subscribe({
      next: () => {
        this.actionLoading = '';
        this.isEditing = false;
        this.showSuccessMessage(this.localeService.translate('common.updatedSuccessfully'));
        this.loadUser();
      },
      error: (error: any) => {
        this.actionLoading = '';
        const errorMessage = error.error?.detail || error.error?.title || 'Failed to update profile. Please try again.';
        this.showErrorMessage(errorMessage);
        console.error('Error updating profile:', error);
      }
    });
  }

  getStatus(): string {
    if (!this.user) return this.localeService.translate('common.noData');
    return this.user.active ? this.localeService.translate('common.active') : this.localeService.translate('common.inactive');
  }

  getRolesDisplay(): string {
    if (!this.user || !this.user.roles) return this.localeService.translate('users.roles');
    return this.user.roles.length > 0 ? this.user.roles.join(', ') : this.localeService.translate('users.roles');
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  getPasswordStrength(): { strength: 'weak' | 'medium' | 'strong', percentage: number } {
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

  isStep1(): boolean {
    return this.currentStep === 1;
  }

  isStep2(): boolean {
    return this.currentStep === 2;
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
}
