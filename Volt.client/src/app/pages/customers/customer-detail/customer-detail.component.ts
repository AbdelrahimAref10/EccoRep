import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AdminCustomerClient, CustomerDto, CustomerState } from '../../../core/services/clientAPI';
import { LocaleService } from '../../../core/services/locale.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe, ConfirmDialogComponent],
  templateUrl: './customer-detail.component.html',
  styleUrls: ['./customer-detail.component.css', '../../../shared/styles/entity-form.css']
})
export class CustomerDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private customerClient = inject(AdminCustomerClient);
  private localeService = inject(LocaleService);

  customer: CustomerDto | null = null;
  customerId = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  actionLoading = '';

  showConfirmDialog = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogType: 'danger' | 'warning' | 'info' = 'warning';
  confirmDialogLoading = false;
  pendingAction:
    | 'activate'
    | 'deactivate'
    | 'block'
    | 'unblock'
    | 'blockCash'
    | 'unblockCash'
    | null = null;

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.customerId = +params['id'];
      if (this.customerId) {
        this.loadCustomer();
      }
    });
  }

  loadCustomer(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.customerClient.getById(this.customerId).subscribe({
      next: (customer: CustomerDto) => {
        this.customer = customer;
        this.isLoading = false;
      },
      error: (error: unknown) => {
        this.errorMessage = this.localeService.translate('customers.failedToLoad');
        this.isLoading = false;
        console.error('Error loading customer:', error);
      }
    });
  }

  onActivate(): void {
    this.openConfirm('activate', 'info');
  }

  onDeactivate(): void {
    this.openConfirm('deactivate', 'warning');
  }

  onBlock(): void {
    this.openConfirm('block', 'danger');
  }

  onUnblock(): void {
    this.openConfirm('unblock', 'info');
  }

  onBlockCash(): void {
    this.openConfirm('blockCash', 'danger');
  }

  onUnblockCash(): void {
    this.openConfirm('unblockCash', 'info');
  }

  onConfirmAction(): void {
    if (!this.pendingAction) return;

    const action = this.pendingAction;
    this.confirmDialogLoading = true;
    this.actionLoading = action;

    const request$ = (() => {
      switch (action) {
        case 'activate':
          return this.customerClient.activate(this.customerId);
        case 'deactivate':
          return this.customerClient.deactivate(this.customerId);
        case 'block':
          return this.customerClient.block(this.customerId);
        case 'unblock':
          return this.customerClient.unblock(this.customerId);
        case 'blockCash':
          return this.customerClient.blockCash(this.customerId);
        case 'unblockCash':
          return this.customerClient.unblockCash(this.customerId);
      }
    })();

    request$.subscribe({
      next: () => {
        this.showConfirmDialog = false;
        this.confirmDialogLoading = false;
        this.pendingAction = null;
        this.actionLoading = '';
        this.showSuccessMessage(this.successKeyFor(action));
        this.loadCustomer();
      },
      error: (error: any) => {
        this.confirmDialogLoading = false;
        this.actionLoading = '';
        this.showConfirmDialog = false;
        this.pendingAction = null;
        this.showErrorMessage(
          error.error?.detail ||
          error.error?.title ||
          this.localeService.translate('common.failedToSave')
        );
      }
    });
  }

  onCancelAction(): void {
    this.showConfirmDialog = false;
    this.confirmDialogLoading = false;
    this.pendingAction = null;
  }

  onBack(): void {
    this.router.navigate(['/main/customers']);
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

  getStateClass(state: CustomerState): string {
    switch (state) {
      case CustomerState.Active:
        return 'customer-detail__status--active';
      case CustomerState.InActive:
        return 'customer-detail__status--inactive';
      case CustomerState.Blocked:
        return 'customer-detail__status--blocked';
      default:
        return '';
    }
  }

  getTypeLabel(registerAs: number | undefined | null): string {
    return registerAs === 1
      ? this.localeService.translate('common.institution')
      : this.localeService.translate('common.individual');
  }

  getVerificationLabel(verificationBy: number | undefined | null): string {
    return verificationBy === 1
      ? this.localeService.translate('common.email')
      : this.localeService.translate('common.phone');
  }

  getGenderLabel(gender: string | undefined | null): string {
    if (!gender) return this.localeService.translate('common.noData');
    const key = gender.toLowerCase();
    if (key === 'male') return this.localeService.translate('common.male');
    if (key === 'female') return this.localeService.translate('common.female');
    return gender;
  }

  showSuccessMessage(messageKey: string): void {
    this.successMessage = this.localeService.translate(messageKey);
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

  isActionLoading(action: string): boolean {
    return this.actionLoading === action;
  }

  canActivate(): boolean {
    return this.customer?.state === CustomerState.InActive;
  }

  canDeactivate(): boolean {
    return this.customer?.state === CustomerState.Active;
  }

  canBlock(): boolean {
    return this.customer?.state === CustomerState.Active;
  }

  canUnblock(): boolean {
    return this.customer?.state === CustomerState.Blocked;
  }

  get CustomerState() {
    return CustomerState;
  }

  get confirmButtonText(): string {
    switch (this.pendingAction) {
      case 'activate':
        return this.localeService.translate('customers.activateAccount');
      case 'deactivate':
        return this.localeService.translate('customers.deactivateAccount');
      case 'block':
        return this.localeService.translate('customers.blockAccount');
      case 'unblock':
        return this.localeService.translate('customers.unblockAccount');
      case 'blockCash':
        return this.localeService.translate('customers.blockCash');
      case 'unblockCash':
        return this.localeService.translate('customers.unblockCash');
      default:
        return this.localeService.translate('common.confirm');
    }
  }

  private openConfirm(
    action: NonNullable<CustomerDetailComponent['pendingAction']>,
    type: 'danger' | 'warning' | 'info'
  ): void {
    this.pendingAction = action;
    this.confirmDialogType = type;
    this.confirmDialogTitle = this.localeService.translate('common.confirm');
    this.confirmDialogMessage = this.localeService.translate(this.confirmKeyFor(action));
    this.showConfirmDialog = true;
  }

  private confirmKeyFor(action: NonNullable<CustomerDetailComponent['pendingAction']>): string {
    switch (action) {
      case 'activate':
        return 'customers.confirmActivate';
      case 'deactivate':
        return 'customers.confirmDeactivate';
      case 'block':
        return 'customers.confirmBlock';
      case 'unblock':
        return 'customers.confirmUnblock';
      case 'blockCash':
        return 'customers.confirmBlockCash';
      case 'unblockCash':
        return 'customers.confirmUnblockCash';
    }
  }

  private successKeyFor(action: NonNullable<CustomerDetailComponent['pendingAction']>): string {
    switch (action) {
      case 'activate':
        return 'customers.activatedSuccessfully';
      case 'deactivate':
        return 'customers.deactivatedSuccessfully';
      case 'block':
        return 'customers.blockedSuccessfully';
      case 'unblock':
        return 'customers.unblockedSuccessfully';
      case 'blockCash':
        return 'customers.cashBlockedSuccessfully';
      case 'unblockCash':
        return 'customers.cashUnblockedSuccessfully';
    }
  }
}
