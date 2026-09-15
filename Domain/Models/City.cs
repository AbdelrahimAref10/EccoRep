using Domain.Common;
using System.Linq;

namespace Domain.Models
{
    public class City : IAuditable
    {
        public int CityId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public decimal? UrgentDelivery { get; private set; }
        public decimal? ServiceFees { get; private set; }
        public decimal? CancellationFees { get; private set; }
        public int? ZoneGroupId { get; private set; }

        public ZoneGroup? ZoneGroup { get; private set; }
        public ICollection<Customer> Customers { get; private set; } = new List<Customer>();
        public ICollection<TieredDiscount> TieredDiscounts { get; private set; } = new List<TieredDiscount>();

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private City() { }

        public static City Create(
            string name,
            string? description = null,
            decimal? urgentDelivery = null,
            decimal? serviceFees = null,
            decimal? cancellationFees = null,
            int? zoneGroupId = null,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("City name cannot be empty", nameof(name));

            if (urgentDelivery.HasValue && urgentDelivery.Value < 0)
                throw new ArgumentException("Urgent delivery fees cannot be negative", nameof(urgentDelivery));
            if (serviceFees.HasValue && serviceFees.Value < 0)
                throw new ArgumentException("Service fees cannot be negative", nameof(serviceFees));
            if (cancellationFees.HasValue && (cancellationFees.Value < 0 || cancellationFees.Value > 100))
                throw new ArgumentException("Cancellation fees must be between 0 and 100 (percentage)", nameof(cancellationFees));
            if (zoneGroupId is <= 0)
                throw new ArgumentException("Zone group ID must be greater than zero", nameof(zoneGroupId));

            return new City
            {
                Name = name.Trim(),
                Description = description,
                IsActive = true,
                UrgentDelivery = urgentDelivery ?? 0,
                ServiceFees = serviceFees ?? 0,
                CancellationFees = cancellationFees ?? 0,
                ZoneGroupId = zoneGroupId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void Update(string name, string? description = null, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("City name cannot be empty", nameof(name));

            Name = name.Trim();
            Description = description;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void AssignZoneGroup(int zoneGroupId, string? modifiedBy = null)
        {
            if (zoneGroupId <= 0)
                throw new ArgumentException("Zone group ID must be greater than zero", nameof(zoneGroupId));

            ZoneGroupId = zoneGroupId;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Activate(string? modifiedBy = null)
        {
            IsActive = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate(string? modifiedBy = null)
        {
            IsActive = false;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdateFees(
            decimal? urgentDelivery,
            decimal? serviceFees,
            decimal? cancellationFees,
            string? modifiedBy = null)
        {
            if (urgentDelivery.HasValue && urgentDelivery.Value < 0)
                throw new ArgumentException("Urgent delivery fees cannot be negative", nameof(urgentDelivery));

            if (serviceFees.HasValue && serviceFees.Value < 0)
                throw new ArgumentException("Service fees cannot be negative", nameof(serviceFees));

            if (cancellationFees.HasValue && (cancellationFees.Value < 0 || cancellationFees.Value > 100))
                throw new ArgumentException("Cancellation fees must be between 0 and 100 (percentage)", nameof(cancellationFees));

            UrgentDelivery = urgentDelivery ?? 0;
            ServiceFees = serviceFees ?? 0;
            CancellationFees = cancellationFees ?? 0;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public decimal CalculateTieredDiscount(int reservationDays)
        {
            if (reservationDays <= 0)
                throw new ArgumentException("Reservation days must be greater than zero", nameof(reservationDays));

            if (TieredDiscounts == null || !TieredDiscounts.Any())
                return 0;

            var applicableDiscount = TieredDiscounts
                .Where(td => reservationDays >= td.From && reservationDays <= td.To)
                .OrderByDescending(td => td.From)
                .FirstOrDefault();

            return applicableDiscount?.Discount ?? 0;
        }
    }
}
