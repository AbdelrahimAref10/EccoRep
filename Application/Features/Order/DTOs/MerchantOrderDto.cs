using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class MerchantOrderDto
    {
        public int MerchantOrderId { get; set; }
        public int OrderId { get; set; }
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public MerchantOrderResponseStatus ResponseStatus { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
