using Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models
{
    public class ApplicationUser : IdentityUser<int>, IAuditable
    {
        public bool Active { get; set; } = true;
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public Customer? Customer { get; set; }
        public Employee? Employee { get; set; }
        public Merchant? Merchant { get; set; }
        public Delivery? Delivery { get; set; }

        public void SetPasswordResetCode(string code, IDateTimeProvider dateTimeProvider, int expiryMinutes = 15)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Password reset code cannot be empty", nameof(code));

            PasswordResetCode = code;
            PasswordResetCodeExpiry = dateTimeProvider.Now.AddMinutes(expiryMinutes);
            LastModifiedDate = dateTimeProvider.Now;
        }

        public bool ValidatePasswordResetCode(string code, IDateTimeProvider dateTimeProvider)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(PasswordResetCode))
                return false;

            if (!PasswordResetCodeExpiry.HasValue || PasswordResetCodeExpiry.Value < dateTimeProvider.Now)
                return false;

            return PasswordResetCode == code;
        }

        public void ClearPasswordResetCode(IDateTimeProvider dateTimeProvider)
        {
            PasswordResetCode = null;
            PasswordResetCodeExpiry = null;
            LastModifiedDate = dateTimeProvider.Now;
        }
    }
}
