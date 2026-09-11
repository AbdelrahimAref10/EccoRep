using Domain.Common;

namespace Domain.Models
{
    /// <summary>Per-vehicle delivery fee snapshot built at Assign Delivery.</summary>
    public class DeliveryOrderPaymentDetail : IAuditable
    {
        public int DeliveryOrderPaymentDetailId { get; private set; }
        public int OrderId { get; private set; }
        public int DeliveryId { get; private set; }
        public int VehicleId { get; private set; }
        public decimal DeliveryFeeShare { get; private set; }

        public Order Order { get; private set; } = null!;
        public Delivery Delivery { get; private set; } = null!;
        public Vehicle Vehicle { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private DeliveryOrderPaymentDetail() { }

        public static DeliveryOrderPaymentDetail Create(
            int orderId,
            int deliveryId,
            int vehicleId,
            decimal deliveryFeeShare,
            string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));
            if (deliveryId <= 0)
                throw new ArgumentException("Delivery ID must be greater than zero", nameof(deliveryId));
            if (vehicleId <= 0)
                throw new ArgumentException("Vehicle ID must be greater than zero", nameof(vehicleId));
            if (deliveryFeeShare < 0)
                throw new ArgumentException("Delivery fee share cannot be negative", nameof(deliveryFeeShare));

            return new DeliveryOrderPaymentDetail
            {
                OrderId = orderId,
                DeliveryId = deliveryId,
                VehicleId = vehicleId,
                DeliveryFeeShare = deliveryFeeShare,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }
    }
}
