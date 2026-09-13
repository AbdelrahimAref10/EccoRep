using Domain.Common;
using Domain.Models;

namespace Domain.Events
{
    /// <summary>Generic ledger post (PayPal capture, non-delivery fault, etc.).</summary>
    public sealed class OrderLedgerPostsRequested : IDomainEvent
    {
        public int OrderId { get; }
        public IReadOnlyList<OrderLedgerLine> Lines { get; }
        public string? CreatedBy { get; }
        public DateTime OccurredOn { get; }

        public OrderLedgerPostsRequested(int orderId, IReadOnlyList<OrderLedgerLine> lines, string? createdBy = null)
        {
            OrderId = orderId;
            Lines = lines ?? Array.Empty<OrderLedgerLine>();
            CreatedBy = createdBy;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Delivery received the vehicle from the merchant.
    /// If the merchant is CashOnReceive, <see cref="Lines"/> contains the merchant debit.
    /// </summary>
    public sealed class OrderVehicleReceivedFromOwnerEvent : IDomainEvent
    {
        public int OrderId { get; }
        public int VehicleId { get; }
        public bool BecameOnWay { get; }
        public bool MerchantCashOnReceive { get; }
        public IReadOnlyList<OrderLedgerLine> Lines { get; }
        public string? CreatedBy { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OrderVehicleReceivedFromOwnerEvent(
            int orderId,
            int vehicleId,
            bool becameOnWay,
            bool merchantCashOnReceive,
            IReadOnlyList<OrderLedgerLine> lines,
            string? createdBy = null)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            BecameOnWay = becameOnWay;
            MerchantCashOnReceive = merchantCashOnReceive;
            Lines = lines ?? Array.Empty<OrderLedgerLine>();
            CreatedBy = createdBy;
        }
    }

    /// <summary>Vehicle delivered to the customer — accrual journals are in <see cref="Lines"/>.</summary>
    public sealed class OrderVehicleDeliveredToCustomerEvent : IDomainEvent
    {
        public int OrderId { get; }
        public int VehicleId { get; }
        public bool BecameCustomerReceived { get; }
        public IReadOnlyList<OrderLedgerLine> Lines { get; }
        public string? CreatedBy { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OrderVehicleDeliveredToCustomerEvent(
            int orderId,
            int vehicleId,
            bool becameCustomerReceived,
            IReadOnlyList<OrderLedgerLine> lines,
            string? createdBy = null)
        {
            OrderId = orderId;
            VehicleId = vehicleId;
            BecameCustomerReceived = becameCustomerReceived;
            Lines = lines ?? Array.Empty<OrderLedgerLine>();
            CreatedBy = createdBy;
        }
    }
}
