using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
    {
        public void Configure(EntityTypeBuilder<Merchant> builder)
        {
            builder.ToTable("VO_Merchant");

            builder.HasKey(m => m.MerchantId);

            builder.Property(m => m.MerchantId)
                .HasColumnName("MerchantId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(m => m.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(m => m.FullName)
                .HasColumnName("FullName")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(m => m.MobileNumber)
                .HasColumnName("MobileNumber")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(m => m.Email)
                .HasColumnName("Email")
                .HasMaxLength(256);

            builder.Property(m => m.PersonalImage)
                .HasColumnName("PersonalImage");

            builder.Property(m => m.InvitationCode)
                .HasColumnName("InvitationCode")
                .HasMaxLength(10);

            builder.Property(m => m.InvitationCodeExpiry)
                .HasColumnName("InvitationCodeExpiry");

            builder.Property(m => m.IsInvitationCodeUsed)
                .HasColumnName("IsInvitationCodeUsed")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(m => m.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(m => m.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(m => m.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(m => m.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(m => m.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            builder.HasOne(m => m.User)
                .WithOne(u => u.Merchant)
                .HasForeignKey<Merchant>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Merchant_UserId");

            builder.HasIndex(m => m.MobileNumber)
                .HasDatabaseName("IX_Merchant_MobileNumber");
        }
    }
}
