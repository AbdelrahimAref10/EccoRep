namespace Application.Features.Order.DTOs
{
    public class DeliveryMenOrderDto
    {
        public int DeliveryMenOrderId { get; set; }
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public int DeliveryId { get; set; }
        public string DeliveryName { get; set; } = string.Empty;
        public bool DeliveryReceivedFromMerchant { get; set; }
        public DateTime? ReceivedFromMerchantAt { get; set; }
    }
}
