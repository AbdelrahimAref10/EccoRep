using Domain.Common;

namespace Domain.Models
{
    public class Permission : IAuditable
    {
        // Private setters for encapsulation
        public int PermissionId { get; private set; }
        public string PermissionName { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Module { get; private set; } = string.Empty; // e.g., "Products", "Orders", "Users"
        public bool IsActive { get; private set; } = true;

        // Navigation property
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Factory method for creating permissions
        public static Permission Create(
            string permissionName,
            string description,
            string module,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                throw new ArgumentException("Permission name cannot be empty", nameof(permissionName));

            if (string.IsNullOrWhiteSpace(module))
                throw new ArgumentException("Module cannot be empty", nameof(module));

            return new Permission
            {
                PermissionName = permissionName,
                Description = description ?? string.Empty,
                Module = module,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        // Domain methods
        public void Update(string permissionName, string description, string module, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                throw new ArgumentException("Permission name cannot be empty", nameof(permissionName));

            if (string.IsNullOrWhiteSpace(module))
                throw new ArgumentException("Module cannot be empty", nameof(module));

            PermissionName = permissionName;
            Description = description ?? string.Empty;
            Module = module;
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
    }
}

