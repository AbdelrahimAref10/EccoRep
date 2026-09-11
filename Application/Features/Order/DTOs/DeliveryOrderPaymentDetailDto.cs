namespace Application.Features.Order.DTOs
{
    public class DeliveryOrderPaymentDetailDto
    {
        public int DeliveryOrderPaymentDetailId { get; set; }
        public int OrderId { get; set; }
        public int DeliveryId { get; set; }
        public string DeliveryName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public decimal DeliveryFeeShare { get; set; }
    }
}
