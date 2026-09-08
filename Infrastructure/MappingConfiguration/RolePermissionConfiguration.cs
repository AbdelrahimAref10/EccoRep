using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("VO_RolePermission");

            // Configure composite primary key
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Configure properties
            builder.Property(rp => rp.RoleId)
                .HasColumnName("RoleId")
                .IsRequired();

            builder.Property(rp => rp.PermissionId)
                .HasColumnName("PermissionId")
                .IsRequired();

            // Configure audit properties
            builder.Property(rp => rp.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(rp => rp.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(rp => rp.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(rp => rp.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Configure indexes
            builder.HasIndex(rp => rp.RoleId)
                .HasDatabaseName("IX_RolePermissions_RoleId");

            builder.HasIndex(rp => rp.PermissionId)
                .HasDatabaseName("IX_RolePermissions_PermissionId");

            // Unique constraint to prevent duplicate role-permission pairs
            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId")
                .IsUnique();
        }
    }
}

