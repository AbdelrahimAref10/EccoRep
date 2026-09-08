using Domain.Common;

namespace Domain.Models
{
    public class TieredDiscount : IAuditable
    {
        // Private setters for encapsulation
        public int Id { get; private set; }
        public int CityId { get; private set; }
        public decimal From { get; private set; }
        public decimal To { get; private set; }
        public decimal Discount { get; private set; } // Percentage value (e.g., 5.0 means 5%)

        // Navigation property
        public City City { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private TieredDiscount() { }

        // Factory method for creating tiered discounts
        public static TieredDiscount Create(
            int cityId,
            decimal from,
            decimal to,
            decimal discount,
            string? createdBy = null)
        {
            if (cityId <= 0)
                throw new ArgumentException("City ID must be greater than zero", nameof(cityId));

            if (from < 0)
                throw new ArgumentException("From days cannot be negative", nameof(from));

            if (to < from)
                throw new ArgumentException("To days must be greater than or equal to From days", nameof(to));

            if (discount < 0 || discount > 100)
                throw new ArgumentException("Discount must be between 0 and 100", nameof(discount));

            return new TieredDiscount
            {
                CityId = cityId,
                From = from,
                To = to,
                Discount = discount,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain method to update tiered discount
        public void Update(
            decimal from,
            decimal to,
            decimal discount,
            string? modifiedBy = null)
        {
            if (from < 0)
                throw new ArgumentException("From days cannot be negative", nameof(from));

            if (to < from)
                throw new ArgumentException("To days must be greater than or equal to From days", nameof(to));

            if (discount < 0 || discount > 100)
                throw new ArgumentException("Discount must be between 0 and 100", nameof(discount));

            From = from;
            To = to;
            Discount = discount;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

