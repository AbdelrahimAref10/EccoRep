using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<int>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserRole<int>> builder)
        {
            builder.ToTable("VO_UserRole");

            // Configure composite primary key
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            // Configure properties
            builder.Property(ur => ur.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(ur => ur.RoleId)
                .HasColumnName("RoleId")
                .IsRequired();
        }
    }
}

