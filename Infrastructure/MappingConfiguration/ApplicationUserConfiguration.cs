using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("VO_User");

            // Configure primary key
            builder.HasKey(u => u.Id);

            // Configure properties
            builder.Property(u => u.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(u => u.UserName)
                .HasColumnName("UserName")
                .HasMaxLength(256);

            builder.Property(u => u.NormalizedUserName)
                .HasColumnName("NormalizedUserName")
                .HasMaxLength(256);

            builder.Property(u => u.Email)
                .HasColumnName("Email")
                .HasMaxLength(256);

            builder.Property(u => u.NormalizedEmail)
                .HasColumnName("NormalizedEmail")
                .HasMaxLength(256);

            builder.Property(u => u.EmailConfirmed)
                .HasColumnName("EmailConfirmed")
                .HasDefaultValue(false);

            builder.Property(u => u.PasswordHash)
                .HasColumnName("PasswordHash");

            builder.Property(u => u.SecurityStamp)
                .HasColumnName("SecurityStamp");

            builder.Property(u => u.ConcurrencyStamp)
                .HasColumnName("ConcurrencyStamp");

            builder.Property(u => u.PhoneNumber)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(50);

            builder.Property(u => u.PhoneNumberConfirmed)
                .HasColumnName("PhoneNumberConfirmed")
                .HasDefaultValue(false);

            builder.Property(u => u.TwoFactorEnabled)
                .HasColumnName("TwoFactorEnabled")
                .HasDefaultValue(false);

            builder.Property(u => u.LockoutEnd)
                .HasColumnName("LockoutEnd");

            builder.Property(u => u.LockoutEnabled)
                .HasColumnName("LockoutEnabled")
                .HasDefaultValue(true);

            builder.Property(u => u.AccessFailedCount)
                .HasColumnName("AccessFailedCount")
                .HasDefaultValue(0);

            builder.Property(u => u.Active)
                .HasColumnName("Active")
                .HasDefaultValue(true);

            builder.Property(u => u.PasswordResetCode)
                .HasColumnName("PasswordResetCode")
                .HasMaxLength(10);

            builder.Property(u => u.PasswordResetCodeExpiry)
                .HasColumnName("PasswordResetCodeExpiry");

            // Configure audit properties
            builder.Property(u => u.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(u => u.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(u => u.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(u => u.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure indexes
            builder.HasIndex(u => u.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique()
                .HasFilter("[NormalizedUserName] IS NOT NULL");

            builder.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("EmailIndex");
        }
    }
}

