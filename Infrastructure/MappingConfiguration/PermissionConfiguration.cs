using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("VO_Permission");

            // Configure primary key
            builder.HasKey(p => p.PermissionId);

            // Configure properties
            builder.Property(p => p.PermissionId)
                .HasColumnName("PermissionId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(p => p.PermissionName)
                .HasColumnName("PermissionName")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasColumnName("Description")
                .HasMaxLength(500);

            builder.Property(p => p.Module)
                .HasColumnName("Module")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true)
                .IsRequired();

            // Configure audit properties
            builder.Property(p => p.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(p => p.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(p => p.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(p => p.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasMany(p => p.RolePermissions)
                .WithOne(rp => rp.Permission)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(p => p.PermissionName)
                .HasDatabaseName("IX_Permissions_PermissionName")
                .IsUnique();

            builder.HasIndex(p => p.Module)
                .HasDatabaseName("IX_Permissions_Module");

            builder.HasIndex(p => new { p.Module, p.IsActive })
                .HasDatabaseName("IX_Permissions_Module_IsActive");
        }
    }
}

