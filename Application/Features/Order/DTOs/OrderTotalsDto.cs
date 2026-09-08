namespace Application.Features.Order.DTOs
{
    public class OrderTotalsDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ServiceFees { get; set; }
        public decimal DeliveryFees { get; set; }
        public decimal UrgentFees { get; set; }
        public decimal TieredDiscount { get; set; }
        public decimal TotalAfterAllFees { get; set; }
    }
}
