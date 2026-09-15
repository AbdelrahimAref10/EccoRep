using Domain.Enums;

namespace Application.Features.Order.Services
{
    public sealed class VehicleReservationAvailabilityItem
    {
        public int VehicleId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string VehicleCode { get; init; } = string.Empty;
        public string? ImagePath { get; init; }
        public VehicleStatus VehicleStatus { get; init; }
        public int MerchantId { get; init; }
        public int MerchantZoneId { get; init; }
        public string MerchantName { get; init; } = string.Empty;
        public string Color { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int? SpeedKmh { get; init; }
        public int? EngineCapacityCc { get; init; }
        public VehicleAvailabilityStatus AvailabilityStatus { get; init; }
        public IReadOnlyList<DateTime> ConflictingDates { get; init; } = Array.Empty<DateTime>();
    }
}
