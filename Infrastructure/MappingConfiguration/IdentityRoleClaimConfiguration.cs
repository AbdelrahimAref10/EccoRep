using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<int>>
    {
        public void Configure(EntityTypeBuilder<IdentityRoleClaim<int>> builder)
        {
            builder.ToTable("VO_RoleClaim");

            // Configure primary key
            builder.HasKey(rc => rc.Id);

            // Configure properties
            builder.Property(rc => rc.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(rc => rc.RoleId)
                .HasColumnName("RoleId")
                .IsRequired();

            builder.Property(rc => rc.ClaimType)
                .HasColumnName("ClaimType")
                .HasMaxLength(256);

            builder.Property(rc => rc.ClaimValue)
                .HasColumnName("ClaimValue")
                .HasMaxLength(256);
        }
    }
}

