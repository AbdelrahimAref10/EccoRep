using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class CustomerWalletDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? OrderId { get; set; }
        public decimal Withdraw { get; set; }
        public decimal Deposit { get; set; }
        public string Description { get; set; } = string.Empty;
        public WalletType Type { get; set; }
        public CustomerWalletState State { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
