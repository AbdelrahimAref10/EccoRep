using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class MerchantNotificationConfiguration : IEntityTypeConfiguration<MerchantNotification>
    {
        public void Configure(EntityTypeBuilder<MerchantNotification> builder)
        {
            builder.ToTable("VO_MerchantNotification");

            builder.HasKey(n => n.MerchantNotificationId);

            builder.Property(n => n.MerchantNotificationId).ValueGeneratedOnAdd();
            builder.Property(n => n.Title).HasMaxLength(500).IsRequired();
            builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
            builder.Property(n => n.NotificationType).IsRequired();
            builder.Property(n => n.IsRead).IsRequired().HasDefaultValue(false);
            builder.Property(n => n.CreatedBy).HasMaxLength(256);
            builder.Property(n => n.LastModifiedBy).HasMaxLength(256);
            builder.Property(n => n.CreatedDate).IsRequired();
            builder.Property(n => n.LastModifiedDate).IsRequired();

            builder.HasOne(n => n.Merchant)
                .WithMany()
                .HasForeignKey(n => n.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.Order)
                .WithMany()
                .HasForeignKey(n => n.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(n => n.MerchantId);
            builder.HasIndex(n => n.IsRead);
            builder.HasIndex(n => n.CreatedDate);
            builder.HasIndex(n => n.OrderId);
        }
    }
}
