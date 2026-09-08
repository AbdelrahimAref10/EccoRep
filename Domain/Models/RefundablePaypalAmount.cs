using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class RefundablePaypalAmount : IAuditable
    {
        // Private setters for encapsulation
        public int Id { get; private set; }
        public int CustomerId { get; private set; }
        public int OrderId { get; private set; }
        public decimal OrderTotal { get; private set; }
        public decimal CancellationFees { get; private set; }
        public decimal RefundableAmount { get; private set; }
        public RefundState State { get; private set; } = RefundState.Pending;

        // Navigation properties
        public Customer Customer { get; private set; } = null!;
        public Order Order { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private RefundablePaypalAmount() { }

        // Factory method for creating refundable amounts
        public static RefundablePaypalAmount Create(
            int customerId,
            int orderId,
            decimal orderTotal,
            decimal cancellationFees,
            string? createdBy = null,
            decimal previousDebt = 0)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than zero", nameof(customerId));

            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));

            if (orderTotal < 0)
                throw new ArgumentException("Order total cannot be negative", nameof(orderTotal));

            if (cancellationFees < 0)
                throw new ArgumentException("Cancellation fees cannot be negative", nameof(cancellationFees));

            if (previousDebt < 0)
                throw new ArgumentException("Previous debt cannot be negative", nameof(previousDebt));

            // Refund only the rental portion after cancellation fee; previous debt was settled and is not refunded.
            var refundableAmount = orderTotal - previousDebt - cancellationFees;
            if (refundableAmount < 0)
                throw new ArgumentException("Refundable amount cannot be negative", nameof(refundableAmount));

            return new RefundablePaypalAmount
            {
                CustomerId = customerId,
                OrderId = orderId,
                OrderTotal = orderTotal,
                CancellationFees = cancellationFees,
                RefundableAmount = refundableAmount,
                State = RefundState.Pending,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void MarkAsSuccess(string? modifiedBy = null)
        {
            State = RefundState.Success;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkAsFailed(string? modifiedBy = null)
        {
            State = RefundState.Failed;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

