namespace Infrastructure.Services
{
    public interface IPayPalService
    {
        Task<PayPalPaymentResult> ProcessPaymentAsync(string orderId, decimal amount, string currency = "USD");
        Task<PayPalCreateOrderResult> CreatePayPalOrderAsync(string orderId, decimal amount, string currency = "EUR");
        Task<PayPalCaptureResult> CapturePayPalOrderAsync(string paypalOrderId);
    }

    public class PayPalPaymentResult
    {
        public bool IsSuccess { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PayPalCreateOrderResult
    {
        public bool IsSuccess { get; set; }
        public string? PayPalOrderId { get; set; }
        public string? ApproveLink { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PayPalCaptureResult
    {
        public bool IsSuccess { get; set; }
        public string? TransactionId { get; set; }
        public string? PayPalOrderId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
