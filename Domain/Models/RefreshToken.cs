using Domain.Common;

namespace Domain.Models
{
    public class RefreshToken : IAuditable
    {
        public int RefreshTokenId { get; private set; }
        public int UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public bool IsUsed { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? ReplacedByToken { get; private set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation property
        public ApplicationUser User { get; private set; } = null!;

        private RefreshToken() { } // EF Core

        public static RefreshToken Create(
            int userId,
            string token,
            DateTime expiresAt,
            string createdBy,
            IDateTimeProvider dateTimeProvider)
        {
            var now = dateTimeProvider.Now;
            return new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = expiresAt,
                IsRevoked = false,
                IsUsed = false,
                CreatedBy = createdBy,
                CreatedDate = now,
                LastModifiedDate = now
            };
        }

        public void Revoke(IDateTimeProvider dateTimeProvider, string? replacedByToken = null)
        {
            var now = dateTimeProvider.Now;
            IsRevoked = true;
            RevokedAt = now;
            ReplacedByToken = replacedByToken;
            LastModifiedDate = now;
        }

        public void MarkAsUsed(IDateTimeProvider dateTimeProvider)
        {
            IsUsed = true;
            LastModifiedDate = dateTimeProvider.Now;
        }

        public bool IsExpired(IDateTimeProvider dateTimeProvider) => dateTimeProvider.Now >= ExpiresAt;
        public bool IsActive(IDateTimeProvider dateTimeProvider) => !IsRevoked && !IsUsed && !IsExpired(dateTimeProvider);
    }
}

