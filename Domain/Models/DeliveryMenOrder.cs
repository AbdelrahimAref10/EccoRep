using Domain.Common;

namespace Domain.Models
{
    /// <summary>Per-vehicle delivery assignment; merchant marks handover via DeliveryReceivedFromMerchant.</summary>
    public class DeliveryMenOrder : IAuditable
    {
        public int DeliveryMenOrderId { get; private set; }
        public int OrderId { get; private set; }
        public int VehicleId { get; private set; }
        public int DeliveryId { get; private set; }
        public bool DeliveryReceivedFromMerchant { get; private set; }
        public DateTime? ReceivedFromMerchantAt { get; private set; }

        public Order Order { get; private set; } = null!;
        public Vehicle Vehicle { get; private set; } = null!;
        public Delivery Delivery { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private DeliveryMenOrder() { }

        public static DeliveryMenOrder Create(
            int orderId,
            int vehicleId,
            int deliveryId,
            string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));
            if (vehicleId <= 0)
                throw new ArgumentException("Vehicle ID must be greater than zero", nameof(vehicleId));
            if (deliveryId <= 0)
                throw new ArgumentException("Delivery ID must be greater than zero", nameof(deliveryId));

            return new DeliveryMenOrder
            {
                OrderId = orderId,
                VehicleId = vehicleId,
                DeliveryId = deliveryId,
                DeliveryReceivedFromMerchant = false,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void MarkReceivedFromMerchant(string? modifiedBy = null)
        {
            if (DeliveryReceivedFromMerchant)
                return;

            DeliveryReceivedFromMerchant = true;
            ReceivedFromMerchantAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
