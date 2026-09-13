namespace Application.Features.Order.DTOs
{
    public class AdminAvailableVehiclesDto
    {
        public int SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int Days { get; set; }
        public int AvailableCount { get; set; }
        public int UnavailableCount { get; set; }
        public List<AdminAvailableVehicleItemDto> Vehicles { get; set; } = new();
    }

    public class AdminAvailableVehicleItemDto
    {
        public int VehicleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        /// <summary>VehicleStatus as int: Available=0, UnderMaintenance=1, Rented=2.</summary>
        public int Status { get; set; }
        public bool IsAvailable { get; set; }
        /// <summary>Null when available. Otherwise: Reserved</summary>
        public string? UnavailableReason { get; set; }
        /// <summary>Dates inside the requested range that conflict (StillBooked only).</summary>
        public List<DateTime> ConflictingDates { get; set; } = new();
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        /// <summary>Top speed in km/h. Optional.</summary>
        public int? SpeedKmh { get; set; }
        /// <summary>Motor engine capacity in CC. Optional.</summary>
        public int? EngineCapacityCc { get; set; }
    }
}
