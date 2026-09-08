using Domain.Enums;

namespace Application.Features.AdminReport.DTOs
{
    public class OrdersDetailsReportDto
    {
        public List<OrdersDetailsReportRowDto> Items { get; set; } = new();
        public OrdersDetailsReportTotalsDto Totals { get; set; } = new();
    }

    public class OrdersDetailsReportRowDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string SubCategoryName { get; set; } = string.Empty;
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int VehiclesCount { get; set; }
        public decimal OrderSubTotal { get; set; }
        public decimal PreviousDebt { get; set; }
        public decimal OrderTotal { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentState? PaymentState { get; set; }
        public OrderState OrderState { get; set; }
        public bool MoneyRefunded { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class OrdersDetailsReportTotalsDto
    {
        public int OrdersCount { get; set; }
        public decimal TotalSubTotal { get; set; }
        public decimal TotalPreviousDebt { get; set; }
        public decimal TotalOrderAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }

    public class CancelledOrdersReportDto
    {
        public List<CancelledOrdersReportRowDto> Items { get; set; } = new();
        public CancelledOrdersReportTotalsDto Totals { get; set; } = new();
    }

    public class CancelledOrdersReportRowDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
        public decimal PreviousDebt { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public OrderState OrderState { get; set; }
        public decimal CancellationFees { get; set; }
        public CustomerWalletState? CancellationFeeState { get; set; }
        public bool CancellationFeePaid { get; set; }
        public decimal RefundablePaypalAmount { get; set; }
        public RefundState? PaypalRefundState { get; set; }
        public bool MoneyRefunded { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CancelledDate { get; set; }
    }

    public class CancelledOrdersReportTotalsDto
    {
        public int OrdersCount { get; set; }
        public decimal TotalOrderAmount { get; set; }
        public decimal TotalCancellationFees { get; set; }
        public decimal TotalPaidCancellationFees { get; set; }
        public decimal TotalUnpaidCancellationFees { get; set; }
        public decimal TotalRefundablePaypal { get; set; }
    }

    public class CancellationDebtsReportDto
    {
        public List<CancellationDebtsReportRowDto> Items { get; set; } = new();
        public CancellationDebtsReportTotalsDto Totals { get; set; } = new();
    }

    public class CancellationDebtsReportRowDto
    {
        public int WalletId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public decimal Amount { get; set; }
        public CustomerWalletState State { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class CancellationDebtsReportTotalsDto
    {
        public int EntriesCount { get; set; }
        public int CustomersCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalUnderPayment { get; set; }
        public decimal TotalPaid { get; set; }
    }

    public class PaymentsReportDto
    {
        public List<PaymentsReportRowDto> Items { get; set; } = new();
        public PaymentsReportTotalsDto Totals { get; set; } = new();
    }

    public class PaymentsReportRowDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentState State { get; set; }
        public decimal Amount { get; set; }
        public decimal PreviousDebt { get; set; }
        public OrderState OrderState { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PaymentsReportTotalsDto
    {
        public int PaymentsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalFailed { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal TotalCashPaid { get; set; }
        public decimal TotalPayPalPaid { get; set; }
    }

    public class PayPalRefundsReportDto
    {
        public List<PayPalRefundsReportRowDto> Items { get; set; } = new();
        public PayPalRefundsReportTotalsDto Totals { get; set; } = new();
    }

    public class PayPalRefundsReportRowDto
    {
        public int RefundId { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
        public decimal CancellationFees { get; set; }
        public decimal RefundableAmount { get; set; }
        public RefundState State { get; set; }
        public bool MoneyRefunded { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PayPalRefundsReportTotalsDto
    {
        public int EntriesCount { get; set; }
        public decimal TotalOrderAmount { get; set; }
        public decimal TotalCancellationFees { get; set; }
        public decimal TotalRefundable { get; set; }
        public decimal TotalPendingRefundable { get; set; }
        public decimal TotalCompletedRefundable { get; set; }
    }
}
