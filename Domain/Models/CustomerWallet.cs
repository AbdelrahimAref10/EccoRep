using Domain.Enums;

namespace Domain.Models
{
    public class CustomerWallet
    {
        public int Id { get; private set; }
        public int CustomerId { get; private set; }
        public int? OrderId { get; private set; }
        public decimal Withdraw { get; private set; }
        public decimal Deposit { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public WalletType Type { get; private set; }
        public CustomerWalletState State { get; private set; } = CustomerWalletState.Pending;
        public DateTime CreatedDate { get; set; }

        // Navigation property
        public Customer Customer { get; private set; } = null!;

        private CustomerWallet() { }

        public static CustomerWallet Create(
            int customerId,
            decimal withdraw,
            decimal deposit,
            string description,
            WalletType type,
            int? orderId = null)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than zero", nameof(customerId));

            if (withdraw < 0)
                throw new ArgumentException("Withdraw cannot be negative", nameof(withdraw));

            if (deposit < 0)
                throw new ArgumentException("Deposit cannot be negative", nameof(deposit));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty", nameof(description));

            return new CustomerWallet
            {
                CustomerId = customerId,
                OrderId = orderId,
                Withdraw = withdraw,
                Deposit = deposit,
                Description = description.Trim(),
                Type = type,
                State = CustomerWalletState.Pending,
                CreatedDate = DateTime.UtcNow
            };
        }

        public void MarkAsPaid()
        {
            State = CustomerWalletState.Paid;
        }

        public void MarkAsUnderPayment(int settleOnOrderId)
        {
            if (State != CustomerWalletState.Pending)
                throw new InvalidOperationException($"Cannot mark wallet as UnderPayment from {State} state.");

            if (settleOnOrderId <= 0)
                throw new ArgumentException("Settle-on order ID must be greater than zero", nameof(settleOnOrderId));

            OrderId = settleOnOrderId;
            State = CustomerWalletState.UnderPayment;
        }

        public void RevertToPending()
        {
            if (State != CustomerWalletState.UnderPayment)
                return;

            State = CustomerWalletState.Pending;
        }
    }
}
