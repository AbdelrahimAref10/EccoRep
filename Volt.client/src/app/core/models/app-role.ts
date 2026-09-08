/** Mirrors Domain.Enums.AppRole — keep in sync with backend. */
export enum AppRole {
  Customer = 1,
  SuperAdmin = 2,
  Merchant = 3,
  Delivery = 4
}

export const AppRoleNames = {
  Customer: 'Customer',
  SuperAdmin: 'Super Admin',
  Merchant: 'Merchant',
  Delivery: 'Delivery'
} as const;

export function appRoleFromName(roleName: string | null | undefined): AppRole | null {
  switch (roleName) {
    case AppRoleNames.Customer:
      return AppRole.Customer;
    case AppRoleNames.SuperAdmin:
      return AppRole.SuperAdmin;
    case AppRoleNames.Merchant:
      return AppRole.Merchant;
    case AppRoleNames.Delivery:
      return AppRole.Delivery;
    default:
      return null;
  }
}

export function appRoleName(role: AppRole): string {
  switch (role) {
    case AppRole.Customer:
      return AppRoleNames.Customer;
    case AppRole.SuperAdmin:
      return AppRoleNames.SuperAdmin;
    case AppRole.Merchant:
      return AppRoleNames.Merchant;
    case AppRole.Delivery:
      return AppRoleNames.Delivery;
    default:
      return '';
  }
}
