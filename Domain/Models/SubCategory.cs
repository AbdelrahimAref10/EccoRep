using Domain.Common;

namespace Domain.Models
{
    public class SubCategory : IAuditable
    {
        // Private setters for encapsulation
        public int SubCategoryId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string? ImageUrl { get; private set; }
        public bool IsActive { get; private set; } = true;
        public bool IsOffer { get; private set; } = false;
        public decimal Price { get; private set; }

        // Foreign key and navigation property
        public int CategoryId { get; private set; }
        public Category Category { get; private set; } = null!;

        // Navigation property
        public ICollection<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private SubCategory() { }

        // Factory method for creating subcategories
        public static SubCategory Create(
            string name,
            string description,
            int categoryId,
            decimal price,
            string? imageUrl = null,
            bool isOffer = false,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SubCategory name cannot be empty", nameof(name));

            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than zero", nameof(categoryId));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            return new SubCategory
            {
                Name = name.Trim(),
                Description = description ?? string.Empty,
                CategoryId = categoryId,
                Price = price,
                ImageUrl = imageUrl,
                IsActive = true,
                IsOffer = isOffer,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void Update(
            string name,
            string description,
            int categoryId,
            decimal price,
            string? imageUrl = null,
            bool isOffer = false,
            string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SubCategory name cannot be empty", nameof(name));

            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than zero", nameof(categoryId));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            Name = name.Trim();
            Description = description ?? string.Empty;
            CategoryId = categoryId;
            Price = price;
            ImageUrl = imageUrl;
            IsOffer = isOffer;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate(string? modifiedBy = null)
        {
            IsActive = false;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Activate(string? modifiedBy = null)
        {
            IsActive = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdateImage(string? imageUrl, string? modifiedBy = null)
        {
            ImageUrl = imageUrl;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdatePrice(decimal price, string? modifiedBy = null)
        {
            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            Price = price;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

