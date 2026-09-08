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
        public VehicleAvailabilityStatus AvailabilityStatus { get; init; }
        public IReadOnlyList<DateTime> ConflictingDates { get; init; } = Array.Empty<DateTime>();
    }
}
