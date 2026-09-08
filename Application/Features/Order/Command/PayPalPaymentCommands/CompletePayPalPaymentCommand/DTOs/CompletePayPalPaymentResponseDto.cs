namespace Application.Features.Order.Command.PayPalPaymentCommands.CompletePayPalPaymentCommand.DTOs
{
    public class CompletePayPalPaymentResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? TransactionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
