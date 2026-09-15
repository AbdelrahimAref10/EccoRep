using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int DestinationZoneId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int VehiclesCount { get; set; }
        public decimal OrderSubTotal { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal PreviousDebt { get; set; }
        public bool MoneyRefunded { get; set; }
        public string? Notes { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string? HotelPhone { get; set; }
        public bool IsUrgent { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public OrderState OrderState { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PayPalApproveLink { get; set; }
        public string? PayPalOrderId { get; set; }
    }
}

