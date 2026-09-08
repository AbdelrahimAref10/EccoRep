using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<int>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserLogin<int>> builder)
        {
            builder.ToTable("VO_UserLogin");

            // Configure composite primary key
            builder.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });

            // Configure properties
            builder.Property(ul => ul.LoginProvider)
                .HasColumnName("LoginProvider")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(ul => ul.ProviderKey)
                .HasColumnName("ProviderKey")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(ul => ul.ProviderDisplayName)
                .HasColumnName("ProviderDisplayName")
                .HasMaxLength(256);

            builder.Property(ul => ul.UserId)
                .HasColumnName("UserId")
                .IsRequired();
        }
    }
}

