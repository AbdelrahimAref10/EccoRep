namespace Application.Features.Order.DTOs
{
    public class AdminOrderTotalsPreviewDto
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int Days { get; set; }
        public bool IsUrgent { get; set; }
        public int VehiclesCount { get; set; }
        public List<AdminOrderPreviewVehicleDto> Vehicles { get; set; } = new();
        /// <summary>Sum of selected vehicles' daily prices.</summary>
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DeliveryFees { get; set; }
        public decimal ServiceFees { get; set; }
        public decimal UrgentFees { get; set; }
        public decimal TieredDiscountPercentage { get; set; }
        public decimal TieredDiscountAmount { get; set; }
        public decimal PreviousDebt { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class AdminOrderPreviewVehicleDto
    {
        public int VehicleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        /// <summary>Top speed in km/h. Optional.</summary>
        public int? SpeedKmh { get; set; }
        /// <summary>Motor engine capacity in CC. Optional.</summary>
        public int? EngineCapacityCc { get; set; }
        public decimal DeliveryFees { get; set; }
        public bool MerchantCashOnReceive { get; set; }
        public int MerchantZoneId { get; set; }
        public string MerchantZoneName { get; set; } = string.Empty;
    }
}
