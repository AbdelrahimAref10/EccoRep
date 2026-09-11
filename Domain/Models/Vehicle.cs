using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class Vehicle : IAuditable
    {
        // Private setters for encapsulation
        public int VehicleId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string VehicleCode { get; private set; } = string.Empty;
        public string? ImageUrl { get; private set; }
        public VehicleStatus Status { get; private set; }

        // Foreign keys and navigation properties
        public int SubCategoryId { get; private set; }
        public SubCategory SubCategory { get; private set; } = null!;

        public int MerchantId { get; private set; }
        public Merchant Merchant { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private Vehicle() { }

        // Factory method for creating vehicles
        public static Vehicle Create(
            string name,
            string vehicleCode,
            int subCategoryId,
            int merchantId,
            VehicleStatus status,
            string? imageUrl = null,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Vehicle name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(vehicleCode))
                throw new ArgumentException("Vehicle code cannot be empty", nameof(vehicleCode));

            if (subCategoryId <= 0)
                throw new ArgumentException("SubCategory ID must be greater than zero", nameof(subCategoryId));

            if (merchantId <= 0)
                throw new ArgumentException("Merchant ID must be greater than zero", nameof(merchantId));

            if (!Enum.IsDefined(typeof(VehicleStatus), status))
                throw new ArgumentException("Invalid vehicle status", nameof(status));

            return new Vehicle
            {
                Name = name.Trim(),
                VehicleCode = vehicleCode.Trim(),
                SubCategoryId = subCategoryId,
                MerchantId = merchantId,
                Status = status,
                ImageUrl = imageUrl,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void Update(
            string name,
            string vehicleCode,
            int subCategoryId,
            int merchantId,
            VehicleStatus status,
            string? imageUrl = null,
            string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Vehicle name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(vehicleCode))
                throw new ArgumentException("Vehicle code cannot be empty", nameof(vehicleCode));

            if (subCategoryId <= 0)
                throw new ArgumentException("SubCategory ID must be greater than zero", nameof(subCategoryId));

            if (merchantId <= 0)
                throw new ArgumentException("Merchant ID must be greater than zero", nameof(merchantId));

            if (!Enum.IsDefined(typeof(VehicleStatus), status))
                throw new ArgumentException("Invalid vehicle status", nameof(status));

            Name = name.Trim();
            VehicleCode = vehicleCode.Trim();
            SubCategoryId = subCategoryId;
            MerchantId = merchantId;
            Status = status;
            ImageUrl = imageUrl;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdateVehicleCode(string vehicleCode, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(vehicleCode))
                throw new ArgumentException("Vehicle code cannot be empty", nameof(vehicleCode));

            VehicleCode = vehicleCode.Trim();
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdateStatus(VehicleStatus status, string? modifiedBy = null)
        {
            if (!Enum.IsDefined(typeof(VehicleStatus), status))
                throw new ArgumentException("Invalid vehicle status", nameof(status));

            Status = status;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void UpdateImage(string? imageUrl, string? modifiedBy = null)
        {
            ImageUrl = imageUrl;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
