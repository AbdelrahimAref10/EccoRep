using Domain.Common;
using System.Linq;

namespace Domain.Models
{
    public class City : IAuditable
    {
        // Private setters for encapsulation
        public int CityId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public decimal? DeliveryFees { get; private set; } // Amount value (per vehicle)
        public decimal? UrgentDelivery { get; private set; } // Amount value
        public decimal? ServiceFees { get; private set; } // Amount value
        public decimal? CancellationFees { get; private set; } // Percentage value (e.g., 5.0 means 5%)

        // Navigation property - one City has many Customers
        public ICollection<Customer> Customers { get; private set; } = new List<Customer>();

        // Navigation property - one City has many TieredDiscounts
        public ICollection<TieredDiscount> TieredDiscounts { get; private set; } = new List<TieredDiscount>();

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private City() { }

        // Factory method for creating cities
        public static City Create(
            string name,
            string? description = null,
            decimal? deliveryFees = null,
            decimal? urgentDelivery = null,
            decimal? serviceFees = null,
            decimal? cancellationFees = null,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("City name cannot be empty", nameof(name));

            if (deliveryFees.HasValue && deliveryFees.Value < 0)
                throw new ArgumentException("Delivery fees cannot be negative", nameof(deliveryFees));
            if (urgentDelivery.HasValue && urgentDelivery.Value < 0)
                throw new ArgumentException("Urgent delivery fees cannot be negative", nameof(urgentDelivery));
            if (serviceFees.HasValue && serviceFees.Value < 0)
                throw new ArgumentException("Service fees cannot be negative", nameof(serviceFees));
            if (cancellationFees.HasValue && (cancellationFees.Value < 0 || cancellationFees.Value > 100))
                throw new ArgumentException("Cancellation fees must be between 0 and 100 (percentage)", nameof(cancellationFees));

            return new City
            {
                Name = name.Trim(),
                Description = description,
                IsActive = true,
                DeliveryFees = deliveryFees ?? 0,
                UrgentDelivery = urgentDelivery ?? 0,
                ServiceFees = serviceFees ?? 0,
                CancellationFees = cancellationFees ?? 0,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void Update(string name, string? description = null, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("City name cannot be empty", nameof(name));

            Name = name.Trim();
            Description = description;
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
            decimal? deliveryFees,
            decimal? urgentDelivery,
            decimal? serviceFees,
            decimal? cancellationFees,
            string? modifiedBy = null)
        {
            if (deliveryFees.HasValue && deliveryFees.Value < 0)
                throw new ArgumentException("Delivery fees cannot be negative", nameof(deliveryFees));

            if (urgentDelivery.HasValue && urgentDelivery.Value < 0)
                throw new ArgumentException("Urgent delivery fees cannot be negative", nameof(urgentDelivery));

            if (serviceFees.HasValue && serviceFees.Value < 0)
                throw new ArgumentException("Service fees cannot be negative", nameof(serviceFees));

            if (cancellationFees.HasValue && (cancellationFees.Value < 0 || cancellationFees.Value > 100))
                throw new ArgumentException("Cancellation fees must be between 0 and 100 (percentage)", nameof(cancellationFees));

            DeliveryFees = deliveryFees ?? 0;
            UrgentDelivery = urgentDelivery ?? 0;
            ServiceFees = serviceFees ?? 0;
            CancellationFees = cancellationFees ?? 0;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates tiered discount based on reservation day count (inclusive).
        /// Returns the discount percentage that applies to the given number of days.
        /// </summary>
        public decimal CalculateTieredDiscount(int reservationDays)
        {
            if (reservationDays <= 0)
                throw new ArgumentException("Reservation days must be greater than zero", nameof(reservationDays));

            if (TieredDiscounts == null || !TieredDiscounts.Any())
                return 0;

            // Find the tier that matches the reservation length in days
            // Order by From descending to get the highest applicable tier
            var applicableDiscount = TieredDiscounts
                .Where(td => reservationDays >= td.From && reservationDays <= td.To)
                .OrderByDescending(td => td.From)
                .FirstOrDefault();

            return applicableDiscount?.Discount ?? 0;
        }
    }
}

