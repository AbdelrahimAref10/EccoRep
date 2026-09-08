using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    /// <summary>
    /// Cancellation fee info shown on order details when customer cancelled and has fees.
    /// </summary>
    public class OrderCancellationFeeInfoDto
    {
        public int WalletEntryId { get; set; }
        public decimal Amount { get; set; }
        public CustomerWalletState State { get; set; }
    }
}
