using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<int>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserToken<int>> builder)
        {
            builder.ToTable("VO_UserToken");

            // Configure composite primary key
            builder.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });

            // Configure properties
            builder.Property(ut => ut.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(ut => ut.LoginProvider)
                .HasColumnName("LoginProvider")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(ut => ut.Name)
                .HasColumnName("Name")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(ut => ut.Value)
                .HasColumnName("Value");
        }
    }
}

