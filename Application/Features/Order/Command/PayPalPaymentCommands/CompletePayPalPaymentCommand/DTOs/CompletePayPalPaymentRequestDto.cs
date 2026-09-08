namespace Application.Features.Order.Command.PayPalPaymentCommands.CompletePayPalPaymentCommand.DTOs
{
    public class CompletePayPalPaymentRequestDto
    {
        public int OrderId { get; set; }
        public string PayPalOrderId { get; set; } = string.Empty;
    }
}
