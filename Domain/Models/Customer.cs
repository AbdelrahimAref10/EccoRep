using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class Customer : IAuditable
    {
        public int CustomerId { get; private set; }
        public int UserId { get; private set; }
        public string MobileNumber { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string Gender { get; private set; } = string.Empty;
        public string? PersonalImage { get; private set; }
        public string? Email { get; private set; }
        public string? CommercialRegisterImage { get; private set; }
        public int RegisterAs { get; private set; }
        public int VerificationBy { get; private set; }
        public CustomerState State { get; private set; } = CustomerState.InActive;
        public bool CashBlock { get; private set; } = false;
        public string? InvitationCode { get; private set; }
        public DateTime? InvitationCodeExpiry { get; private set; }
        public bool IsInvitationCodeUsed { get; private set; } = false;
        public string? AndriodDevice { get; private set; }
        public string? IosDevice { get; private set; }

        public int CityId { get; private set; }
        public City City { get; private set; } = null!;
        public ApplicationUser User { get; private set; } = null!;
        public CustomerLocation? CustomerLocation { get; private set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private Customer() { }

        public static Customer Create(
            int userId,
            string mobileNumber,
            string fullName,
            string gender,
            string invitationCode,
            int cityId,
            int registerAs,
            int verificationBy,
            string? email = null,
            string? personalImage = null,
            string? commercialRegisterImage = null,
            string? createdBy = null)
        {
            if (userId <= 0)
                throw new ArgumentException("User ID must be greater than zero", nameof(userId));

            if (string.IsNullOrWhiteSpace(mobileNumber))
                throw new ArgumentException("Mobile number cannot be empty", nameof(mobileNumber));

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty", nameof(fullName));

            if (string.IsNullOrWhiteSpace(gender))
                throw new ArgumentException("Gender cannot be empty", nameof(gender));

            if (string.IsNullOrWhiteSpace(invitationCode))
                throw new ArgumentException("Invitation code cannot be empty", nameof(invitationCode));

            if (!Enum.IsDefined(typeof(RegisterAs), registerAs))
                throw new ArgumentException("Invalid RegisterAs value", nameof(registerAs));

            if (!Enum.IsDefined(typeof(VerificationBy), verificationBy))
                throw new ArgumentException("Invalid VerificationBy value", nameof(verificationBy));

            return new Customer
            {
                UserId = userId,
                MobileNumber = mobileNumber,
                FullName = fullName,
                Gender = gender,
                PersonalImage = personalImage,
                Email = email,
                CommercialRegisterImage = commercialRegisterImage,
                RegisterAs = registerAs,
                VerificationBy = verificationBy,
                State = CustomerState.InActive,
                InvitationCode = invitationCode,
                InvitationCodeExpiry = DateTime.UtcNow.AddHours(24),
                IsInvitationCodeUsed = false,
                CityId = cityId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void Activate(string? modifiedBy = null)
        {
            State = CustomerState.Active;
            IsInvitationCodeUsed = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void ActivateWithoutClearingCode(string? modifiedBy = null)
        {
            State = CustomerState.Active;
            IsInvitationCodeUsed = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Block(string? modifiedBy = null)
        {
            State = CustomerState.Blocked;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Unblock(string? modifiedBy = null)
        {
            if (State == CustomerState.Blocked)
            {
                State = CustomerState.Active;
                LastModifiedBy = modifiedBy;
                LastModifiedDate = DateTime.UtcNow;
            }
        }

        public void Deactivate(string? modifiedBy = null)
        {
            State = CustomerState.InActive;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void BlockCashPayment(string? modifiedBy = null)
        {
            CashBlock = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UnblockCashPayment(string? modifiedBy = null)
        {
            CashBlock = false;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public bool ValidateInvitationCode(string code, IDateTimeProvider dateTimeProvider)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (IsInvitationCodeUsed)
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
        }

        public void AddFireBaseDevices(string? androidDevice, string? iosDevice, string? modifiedBy = null)
        {
            if (!string.IsNullOrWhiteSpace(androidDevice))
                AndriodDevice = androidDevice;
            if (!string.IsNullOrWhiteSpace(iosDevice))
                IosDevice = iosDevice;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdateProfile(string fullName, string gender, int cityId, string? email = null, string? personalImage = null, string? commercialRegisterImage = null, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty", nameof(fullName));

            if (string.IsNullOrWhiteSpace(gender))
                throw new ArgumentException("Gender cannot be empty", nameof(gender));

            FullName = fullName;
            Gender = gender;
            CityId = cityId;
            Email = email;
            PersonalImage = personalImage;
            CommercialRegisterImage = commercialRegisterImage;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void SaveLocation(double longitude, double latitude, DateTime lastModifiedDate)
        {
            var location = CustomerLocation;
            if (location is null)
            {
                var newLocation = CustomerLocation.Create(CustomerId, longitude, latitude, lastModifiedDate);
                CustomerLocation = newLocation;
                return;
            }

            location.UpdateLocation(longitude, latitude, lastModifiedDate);
        }
    }
}
