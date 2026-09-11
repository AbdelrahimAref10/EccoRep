namespace Application.Features.Order.DTOs
{
    public class MerchantOrderPaymentDetailDto
    {
        public int MerchantOrderPaymentDetailId { get; set; }
        public int OrderId { get; set; }
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public decimal VehicleRental { get; set; }
        public decimal ServiceFeeShare { get; set; }
        public decimal NetAmount { get; set; }
    }
}
