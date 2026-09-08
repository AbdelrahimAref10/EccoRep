using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Models
{
    public class AdminNotification : IAuditable
    {
        public int AdminNotificationId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public int? OrderId { get; private set; }
        public NotificationType NotificationType { get; private set; }
        public bool IsRead { get; private set; } = false;
        public DateTime? ReadAt { get; private set; }
        public int? ReadByUserId { get; private set; }

        // Navigation properties
        public Order? Order { get; private set; }
        public ApplicationUser? ReadByUser { get; private set; }

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private AdminNotification() { }

        // Factory method
        public static AdminNotification Create(
            string title,
            string message,
            NotificationType notificationType,
            int? orderId = null,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty", nameof(title));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            return new AdminNotification
            {
                Title = title,
                Message = message,
                NotificationType = notificationType,
                OrderId = orderId,
                IsRead = false,
                CreatedBy = createdBy
            };
        }

        // Domain methods
        public void MarkAsRead(int userId, IDateTimeProvider dateTimeProvider)
        {
            if (IsRead)
                return;

            IsRead = true;
            ReadAt = dateTimeProvider.Now;
            ReadByUserId = userId;
            LastModifiedDate = dateTimeProvider.Now;
        }

        public void MarkAsUnread()
        {
            IsRead = false;
            ReadAt = null;
            ReadByUserId = null;
        }
    }
}

