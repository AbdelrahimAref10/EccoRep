using Domain.Enums;
using System.Collections.Generic;

namespace Application.Features.Order.DTOs
{
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobileNumber { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public decimal SubCategoryPrice { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int VehiclesCount { get; set; }
        public decimal OrderSubTotal { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal PreviousDebt { get; set; }
        public bool MoneyRefunded { get; set; }
        public string? Notes { get; set; }
        public string PassportImage { get; set; } = string.Empty; // Base64
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string? HotelPhone { get; set; }
        public bool IsUrgent { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public OrderState OrderState { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<OrderVehicleDto> OrderVehicles { get; set; } = new List<OrderVehicleDto>();
        public List<OrderPaymentDto> OrderPayments { get; set; } = new List<OrderPaymentDto>();
        public RefundablePaypalAmountDto? RefundablePaypalAmount { get; set; }
        public OrderTotalsDto? OrderTotals { get; set; }
        /// <summary>Present when order was cancelled and has a cancellation fee (wallet entry).</summary>
        public OrderCancellationFeeInfoDto? OrderCancellationFee { get; set; }
    }
}

