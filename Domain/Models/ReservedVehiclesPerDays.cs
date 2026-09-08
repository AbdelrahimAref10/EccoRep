using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class ReservedVehiclesPerDays : IAuditable
    {
        // Private setters for encapsulation
        public int Id { get; private set; }
        public int VehicleId { get; private set; }
        public int SubCategoryId { get; private set; }
        public string VehicleCode { get; private set; } = string.Empty;
        public int OrderId { get; private set; }
        public DateTime DateFrom { get; private set; }
        public DateTime DateTo { get; private set; }
        public ReservedVehicleState State { get; private set; } = ReservedVehicleState.StillBooked;

        // Navigation properties
        public Vehicle Vehicle { get; private set; } = null!;
        public SubCategory SubCategory { get; private set; } = null!;
        public Order Order { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private ReservedVehiclesPerDays() { }

        // Factory method for creating reservations
        public static ReservedVehiclesPerDays Create(
            int vehicleId,
            int subCategoryId,
            string vehicleCode,
            int orderId,
            DateTime dateFrom,
            DateTime dateTo,
            string? createdBy = null)
        {
            if (vehicleId <= 0)
                throw new ArgumentException("Vehicle ID must be greater than zero", nameof(vehicleId));

            if (subCategoryId <= 0)
                throw new ArgumentException("SubCategory ID must be greater than zero", nameof(subCategoryId));

            if (string.IsNullOrWhiteSpace(vehicleCode))
                throw new ArgumentException("Vehicle code is required", nameof(vehicleCode));

            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));

            if (dateFrom > dateTo)
                throw new ArgumentException("Date from must be before or equal to date to", nameof(dateFrom));

            return new ReservedVehiclesPerDays
            {
                VehicleId = vehicleId,
                SubCategoryId = subCategoryId,
                VehicleCode = vehicleCode.Trim(),
                OrderId = orderId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                State = ReservedVehicleState.StillBooked,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void Cancel(string? modifiedBy = null)
        {
            State = ReservedVehicleState.Cancelled;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

