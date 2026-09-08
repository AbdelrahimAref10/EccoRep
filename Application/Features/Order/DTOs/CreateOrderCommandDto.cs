namespace Application.Features.Order.DTOs
{
    public class CreateOrderCommandDto
    {
        public int SubCategoryId { get; set; }
        public int CityId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public List<int> VehicleIds { get; set; } = new();
        public string? Notes { get; set; }
        public string PassportImage { get; set; } = string.Empty; // Base64
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string? HotelPhone { get; set; }
        public bool IsUrgent { get; set; }
        public int PaymentMethodId { get; set; } // PaymentMethod enum
        public decimal MobileTotal { get; set; } // Total calculated by mobile app
    }
}
