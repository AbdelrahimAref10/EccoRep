namespace Application.Features.Order.DTOs
{
    public class CustomerAvailableVehicleItemDto
    {
        public int VehicleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        /// <summary>VehicleAvailabilityStatus as int: Available=0, Reserved=1.</summary>
        public int Status { get; set; }
        /// <summary>Dates inside the requested range that conflict (StillBooked only).</summary>
        public List<DateTime> ConflictingDates { get; set; } = new();
    }
}
