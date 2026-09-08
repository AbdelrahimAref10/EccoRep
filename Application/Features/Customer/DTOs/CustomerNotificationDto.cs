using Domain.Enums;

namespace Application.Features.Customer.DTOs
{
    /// <summary>
    /// Customer-facing projection of notifications stored in VO_AdminNotification,
    /// filtered by the customer's orders.
    /// </summary>
    public class CustomerNotificationDto
    {
        public int CustomerNotificationId { get; set; }
        public int CustomerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public NotificationType NotificationType { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
