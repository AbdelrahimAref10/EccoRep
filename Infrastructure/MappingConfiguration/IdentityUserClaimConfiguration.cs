using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<int>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserClaim<int>> builder)
        {
            builder.ToTable("VO_UserClaim");

            // Configure primary key
            builder.HasKey(uc => uc.Id);

            // Configure properties
            builder.Property(uc => uc.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(uc => uc.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(uc => uc.ClaimType)
                .HasColumnName("ClaimType")
                .HasMaxLength(256);

            builder.Property(uc => uc.ClaimValue)
                .HasColumnName("ClaimValue")
                .HasMaxLength(256);
        }
    }
}

