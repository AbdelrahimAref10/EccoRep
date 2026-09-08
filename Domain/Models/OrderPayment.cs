using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class OrderPayment : IAuditable
    {
        // Private setters for encapsulation
        public int Id { get; private set; }
        public int OrderId { get; private set; }
        public int PaymentMethodId { get; private set; } // PaymentMethod enum
        public decimal Total { get; private set; }
        public PaymentState State { get; private set; } = PaymentState.Pending;

        // Navigation property
        public Order Order { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private OrderPayment() { }

        // Factory method for creating payments
        public static OrderPayment Create(
            int orderId,
            int paymentMethodId,
            decimal total,
            string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));

            if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethodId))
                throw new ArgumentException("Invalid payment method", nameof(paymentMethodId));

            if (total < 0)
                throw new ArgumentException("Total cannot be negative", nameof(total));

            return new OrderPayment
            {
                OrderId = orderId,
                PaymentMethodId = paymentMethodId,
                Total = total,
                State = PaymentState.Pending,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void MarkAsPaid(string? modifiedBy = null)
        {
            State = PaymentState.Paid;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkAsFailed(string? modifiedBy = null)
        {
            State = PaymentState.Failed;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkAsRefunded(string? modifiedBy = null)
        {
            State = PaymentState.Refunded;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates payment method/total while still Pending or Failed.
        /// Resets state to Pending when values change.
        /// </summary>
        public void Update(int paymentMethodId, decimal total, string? modifiedBy = null)
        {
            if (State == PaymentState.Paid || State == PaymentState.Refunded)
                throw new InvalidOperationException($"Cannot update payment in {State} state.");

            if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethodId))
                throw new ArgumentException("Invalid payment method", nameof(paymentMethodId));

            if (total < 0)
                throw new ArgumentException("Total cannot be negative", nameof(total));

            PaymentMethodId = paymentMethodId;
            Total = total;
            State = PaymentState.Pending;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

