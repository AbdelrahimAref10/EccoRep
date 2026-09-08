import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RoleClient, RoleDto, CreateRoleCommand, UpdateRoleCommand } from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TranslatePipe, ConfirmDialogComponent],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.css'
})
export class RolesComponent implements OnInit {
  private roleClient = inject(RoleClient);
  private fb = inject(FormBuilder);
  private localeService = inject(LocaleService);

  roles: RoleDto[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  showModal = false;
  roleForm: FormGroup;
  isEditing = false;
  editingRoleId: number | null = null;
  isSubmitting = false;

  showConfirmDialog = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogType: 'danger' | 'warning' | 'info' = 'danger';
  confirmDialogLoading = false;
  pendingDeleteId: number | null = null;

  constructor() {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.pattern(/^[A-Za-z][A-Za-z0-9]*$/)]]
    });
  }

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.roleClient.getAllRoles().subscribe({
      next: (roles: RoleDto[]) => {
        this.roles = roles;
        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('common.failedToLoad');
        this.isLoading = false;
        console.error('Error loading roles:', error);
      }
    });
  }

  onAddNew(): void {
    this.isEditing = false;
    this.editingRoleId = null;
    this.roleForm.reset();
    this.showModal = true;
  }

  onEdit(role: RoleDto): void {
    this.isEditing = true;
    this.editingRoleId = role.roleId;
    this.roleForm.patchValue({
      name: role.roleName || ''
    });
    this.showModal = true;
  }

  onDelete(roleId: number, roleName: string): void {
    this.pendingDeleteId = roleId;
    this.confirmDialogTitle = this.localeService.translate('roles.deleteTitle');
    this.confirmDialogMessage = this.localeService.translate('roles.deleteMessage', { name: roleName });
    this.confirmDialogType = 'danger';
    this.showConfirmDialog = true;
  }

  onCancelDelete(): void {
    this.showConfirmDialog = false;
    this.pendingDeleteId = null;
    this.confirmDialogLoading = false;
  }

  onConfirmDelete(): void {
    if (this.pendingDeleteId == null) return;
    this.confirmDialogLoading = true;
    this.roleClient.deleteRole(this.pendingDeleteId).subscribe({
      next: () => {
        this.showConfirmDialog = false;
        this.confirmDialogLoading = false;
        this.pendingDeleteId = null;
        this.showSuccessMessage(this.localeService.translate('common.deletedSuccessfully'));
        this.loadRoles();
      },
      error: (error: any) => {
        this.confirmDialogLoading = false;
        this.showConfirmDialog = false;
        this.pendingDeleteId = null;
        const errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('roles.failedToDelete');
        this.showErrorMessage(errorMessage);
      }
    });
  }

  onSubmit(): void {
    if (this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formValue = this.roleForm.value;

    if (this.isEditing && this.editingRoleId) {
      const command = new UpdateRoleCommand();
      command.roleId = this.editingRoleId;
      command.roleName = formValue.name;

      this.roleClient.updateRole(command).subscribe({
        next: () => {
          this.showModal = false;
          this.showSuccessMessage(this.localeService.translate('common.updatedSuccessfully'));
          this.loadRoles();
          this.isSubmitting = false;
        },
        error: (error: any) => {
          const errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('roles.failedToUpdate');
          this.showErrorMessage(errorMessage);
          this.isSubmitting = false;
        }
      });
    } else {
      const command = new CreateRoleCommand();
      command.roleName = formValue.name;

      this.roleClient.createRole(command).subscribe({
        next: () => {
          this.showModal = false;
          this.showSuccessMessage(this.localeService.translate('common.createdSuccessfully'));
          this.loadRoles();
          this.isSubmitting = false;
        },
        error: (error: any) => {
          const errorMessage = error.error?.detail || error.error?.title || this.localeService.translate('roles.failedToCreate');
          this.showErrorMessage(errorMessage);
          this.isSubmitting = false;
        }
      });
    }
  }

  onCloseModal(): void {
    this.showModal = false;
    this.roleForm.reset();
    this.isEditing = false;
    this.editingRoleId = null;
    this.isSubmitting = false;
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
