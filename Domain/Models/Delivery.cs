using Domain.Common;

namespace Domain.Models
{
    public class Delivery : IAuditable
    {
        public int DeliveryId { get; private set; }
        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string MobileNumber { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string? PersonalImage { get; private set; }
        public string? InvitationCode { get; private set; }
        public DateTime? InvitationCodeExpiry { get; private set; }
        public bool IsInvitationCodeUsed { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public ApplicationUser User { get; private set; } = null!;

        private Delivery() { }

        public static Delivery Create(
            int userId,
            string fullName,
            string mobileNumber,
            string invitationCode,
            string? email = null,
            string? personalImage = null,
            string? createdBy = null,
            bool isActive = false)
        {
            if (userId <= 0)
                throw new ArgumentException("User ID must be greater than zero", nameof(userId));

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty", nameof(fullName));

            if (string.IsNullOrWhiteSpace(mobileNumber))
                throw new ArgumentException("Mobile number cannot be empty", nameof(mobileNumber));

            if (string.IsNullOrWhiteSpace(invitationCode))
                throw new ArgumentException("Invitation code cannot be empty", nameof(invitationCode));

            return new Delivery
            {
                UserId = userId,
                FullName = fullName.Trim(),
                MobileNumber = mobileNumber.Trim(),
                Email = email,
                PersonalImage = personalImage,
                InvitationCode = invitationCode,
                InvitationCodeExpiry = DateTime.UtcNow.AddHours(24),
                IsInvitationCodeUsed = false,
                IsActive = isActive,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void Activate(string? modifiedBy = null)
        {
            IsActive = true;
            IsInvitationCodeUsed = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public bool ValidateInvitationCode(string code, IDateTimeProvider dateTimeProvider)
        {
            if (string.IsNullOrWhiteSpace(code) || IsInvitationCodeUsed)
                return false;

            if (InvitationCodeExpiry.HasValue && InvitationCodeExpiry.Value < dateTimeProvider.Now)
                return false;

            return InvitationCode == code;
        }

        public void RegenerateInvitationCode(string newCode, IDateTimeProvider dateTimeProvider)
        {
            if (string.IsNullOrWhiteSpace(newCode))
                throw new ArgumentException("Invitation code cannot be empty", nameof(newCode));

            InvitationCode = newCode;
            InvitationCodeExpiry = dateTimeProvider.Now.AddHours(24);
            IsInvitationCodeUsed = false;
        }

        public void UpdateProfile(string fullName, string? email = null, string? personalImage = null, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty", nameof(fullName));

            FullName = fullName.Trim();
            Email = email;
            PersonalImage = personalImage;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
