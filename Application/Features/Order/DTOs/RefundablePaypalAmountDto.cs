using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class RefundablePaypalAmountDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal CancellationFees { get; set; }
        public decimal RefundableAmount { get; set; }
        public RefundState State { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

