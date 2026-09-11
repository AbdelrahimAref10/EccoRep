using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class MerchantNotification : IAuditable
    {
        public int MerchantNotificationId { get; private set; }
        public int MerchantId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public int? OrderId { get; private set; }
        public NotificationType NotificationType { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }

        public Merchant Merchant { get; private set; } = null!;
        public Order? Order { get; private set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private MerchantNotification() { }

        public static MerchantNotification Create(
            int merchantId,
            string title,
            string message,
            NotificationType notificationType,
            int? orderId = null,
            string? createdBy = null)
        {
            if (merchantId <= 0)
                throw new ArgumentException("Merchant ID must be greater than zero", nameof(merchantId));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty", nameof(title));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            return new MerchantNotification
            {
                MerchantId = merchantId,
                Title = title.Trim(),
                Message = message.Trim(),
                NotificationType = notificationType,
                OrderId = orderId,
                IsRead = false,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void MarkAsRead()
        {
            if (IsRead)
                return;

            IsRead = true;
            ReadAt = DateTime.UtcNow;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
