using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class OrderVehicle : IAuditable
    {
        public int OrderId { get; private set; }
        public int VehicleId { get; private set; }

        public bool ReceivedFromOwner { get; private set; }
        public string? ReceivedFromOwnerImageUrl { get; private set; }
        public DateTime? ReceivedFromOwnerAt { get; private set; }

        public bool DeliveredToCustomer { get; private set; }
        public string? DeliveredToCustomerImageUrl { get; private set; }
        public DateTime? DeliveredToCustomerAt { get; private set; }

        public bool ReceivedFromCustomer { get; private set; }
        public string? ReceivedFromCustomerImageUrl { get; private set; }
        public DateTime? ReceivedFromCustomerAt { get; private set; }

        public bool DeliveredToOwner { get; private set; }
        public string? DeliveredToOwnerImageUrl { get; private set; }
        public DateTime? DeliveredToOwnerAt { get; private set; }

        /// <summary>Merchant response for this specific vehicle on the order.</summary>
        public MerchantVehicleResponseStatus MerchantResponseStatus { get; private set; }

        /// <summary>Admin marked vehicle as not received by customer (journals still posted as delivered + fault debit).</summary>
        public bool DeliveryFailed { get; private set; }
        public string? DeliveryFailureReason { get; private set; }
        public Domain.Enums.FaultParty? DeliveryFailureFaultParty { get; private set; }

        public Order Order { get; private set; } = null!;
        public Vehicle Vehicle { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private OrderVehicle() { }

        public static OrderVehicle Create(
            int orderId,
            int vehicleId,
            string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));

            if (vehicleId <= 0)
                throw new ArgumentException("Vehicle ID must be greater than zero", nameof(vehicleId));

            return new OrderVehicle
            {
                OrderId = orderId,
                VehicleId = vehicleId,
                MerchantResponseStatus = MerchantVehicleResponseStatus.Pending,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void ConfirmByMerchant(string? modifiedBy = null)
        {
            MerchantResponseStatus = MerchantVehicleResponseStatus.Confirmed;
            Touch(modifiedBy);
        }

        public void DeclineByMerchant(string? modifiedBy = null)
        {
            MerchantResponseStatus = MerchantVehicleResponseStatus.Declined;
            Touch(modifiedBy);
        }

        public void ResetMerchantResponse(string? modifiedBy = null)
        {
            MerchantResponseStatus = MerchantVehicleResponseStatus.Pending;
            Touch(modifiedBy);
        }

        public void AttachVehicle(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        internal void MarkReceivedFromOwner(string? imageUrl, string? modifiedBy = null)
        {
            if (ReceivedFromOwner)
                return;

            ReceivedFromOwner = true;
            ReceivedFromOwnerImageUrl = imageUrl;
            ReceivedFromOwnerAt = DateTime.UtcNow;
            Touch(modifiedBy);
        }

        internal void MarkDeliveredToCustomer(string? imageUrl, string? modifiedBy = null)
        {
            if (!ReceivedFromOwner)
                throw new InvalidOperationException("Cannot deliver to customer before receiving from owner.");

            if (DeliveredToCustomer)
                return;

            DeliveredToCustomer = true;
            DeliveredToCustomerImageUrl = imageUrl;
            DeliveredToCustomerAt = DateTime.UtcNow;
            Touch(modifiedBy);
        }

        internal void MarkReceivedFromCustomer(string? imageUrl, string? modifiedBy = null)
        {
            if (!DeliveredToCustomer)
                throw new InvalidOperationException("Cannot receive from customer before delivering to customer.");

            if (ReceivedFromCustomer)
                return;

            ReceivedFromCustomer = true;
            ReceivedFromCustomerImageUrl = imageUrl;
            ReceivedFromCustomerAt = DateTime.UtcNow;
            Touch(modifiedBy);
        }

        internal void MarkDeliveredToOwner(string? imageUrl, string? modifiedBy = null)
        {
            if (!ReceivedFromCustomer)
                throw new InvalidOperationException("Cannot deliver to owner before receiving from customer.");

            if (DeliveredToOwner)
                return;

            DeliveredToOwner = true;
            DeliveredToOwnerImageUrl = imageUrl;
            DeliveredToOwnerAt = DateTime.UtcNow;
            Touch(modifiedBy);
        }

        internal void MarkDeliveryFailed(
            string reason,
            Domain.Enums.FaultParty faultParty,
            string? modifiedBy = null)
        {
            if (!ReceivedFromOwner)
                throw new InvalidOperationException("Cannot mark delivery failed before receiving from owner.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Failure reason is required", nameof(reason));

            if (faultParty is Domain.Enums.FaultParty.None or Domain.Enums.FaultParty.Customer)
                throw new ArgumentException("Fault party must be Merchant, Delivery, or Company", nameof(faultParty));

            DeliveryFailed = true;
            DeliveryFailureReason = reason.Trim();
            DeliveryFailureFaultParty = faultParty;

            // Cycle treats vehicle as delivered to customer for aggregation.
            if (!DeliveredToCustomer)
            {
                DeliveredToCustomer = true;
                DeliveredToCustomerAt = DateTime.UtcNow;
            }

            Touch(modifiedBy);
        }

        private void Touch(string? modifiedBy)
        {
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
