namespace Domain.Enums
{
    public enum AppRole
    {
        Customer = 1,
        SuperAdmin = 2,
        Merchant = 3,
        Delivery = 4
    }

    public static class AppRoleNames
    {
        public const string Customer = "Customer";
        public const string SuperAdmin = "Super Admin";
        public const string Merchant = "Merchant";
        public const string Delivery = "Delivery";

        public static readonly string[] All =
        [
            Customer,
            SuperAdmin,
            Merchant,
            Delivery
        ];

        public static string ToRoleName(AppRole role) => role switch
        {
            AppRole.Customer => Customer,
            AppRole.SuperAdmin => SuperAdmin,
            AppRole.Merchant => Merchant,
            AppRole.Delivery => Delivery,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown application role")
        };

        public static bool TryFromInt(int value, out AppRole role)
        {
            if (Enum.IsDefined(typeof(AppRole), value))
            {
                role = (AppRole)value;
                return true;
            }

            role = default;
            return false;
        }

        public static bool IsPublicRegistrationAllowed(AppRole role) =>
            role is AppRole.Customer or AppRole.Merchant or AppRole.Delivery;
    }
}
