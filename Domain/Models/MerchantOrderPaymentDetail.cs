using Domain.Common;

namespace Domain.Models
{
    /// <summary>Per-vehicle merchant payout snapshot built at Confirmed.</summary>
    public class MerchantOrderPaymentDetail : IAuditable
    {
        public int MerchantOrderPaymentDetailId { get; private set; }
        public int OrderId { get; private set; }
        public int MerchantId { get; private set; }
        public int VehicleId { get; private set; }
        public decimal VehicleRental { get; private set; }
        public decimal NetAmount { get; private set; }

        public Order Order { get; private set; } = null!;
        public Merchant Merchant { get; private set; } = null!;
        public Vehicle Vehicle { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private MerchantOrderPaymentDetail() { }

        public static MerchantOrderPaymentDetail Create(
            int orderId,
            int merchantId,
            int vehicleId,
            decimal vehicleRental,
            string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));
            if (merchantId <= 0)
                throw new ArgumentException("Merchant ID must be greater than zero", nameof(merchantId));
            if (vehicleId <= 0)
                throw new ArgumentException("Vehicle ID must be greater than zero", nameof(vehicleId));
            if (vehicleRental < 0)
                throw new ArgumentException("Vehicle rental cannot be negative", nameof(vehicleRental));

            return new MerchantOrderPaymentDetail
            {
                OrderId = orderId,
                MerchantId = merchantId,
                VehicleId = vehicleId,
                VehicleRental = vehicleRental,
                NetAmount = vehicleRental,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }
    }
}
