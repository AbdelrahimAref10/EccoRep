using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class OrderPaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public PaymentState State { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

