using Domain.Enums;

namespace Application.Features.AdminNotification.DTOs
{
    public class AdminNotificationDto
    {
        public int AdminNotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public NotificationType NotificationType { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? ReadByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

