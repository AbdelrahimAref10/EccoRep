using Domain.Common;

namespace Domain.Models
{
    public class RolePermission : IAuditable
    {
        // Composite key properties
        public int RoleId { get; private set; }
        public int PermissionId { get; private set; }

        // Navigation properties
        public ApplicationRole Role { get; private set; } = null!;
        public Permission Permission { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Factory method for creating role-permission relationships
        public static RolePermission Create(
            int roleId,
            int permissionId,
            string? createdBy = null)
        {
            if (roleId <= 0)
                throw new ArgumentException("Role ID must be greater than zero", nameof(roleId));

            if (permissionId <= 0)
                throw new ArgumentException("Permission ID must be greater than zero", nameof(permissionId));

            return new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }
    }
}

