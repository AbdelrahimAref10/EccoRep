using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class AdminNotificationConfiguration : IEntityTypeConfiguration<AdminNotification>
    {
        public void Configure(EntityTypeBuilder<AdminNotification> builder)
        {
            builder.ToTable("VO_AdminNotification");

            builder.HasKey(an => an.AdminNotificationId);

            builder.Property(an => an.AdminNotificationId)
                .HasColumnName("AdminNotificationId")
                .ValueGeneratedOnAdd();

            builder.Property(an => an.Title)
                .HasColumnName("Title")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(an => an.Message)
                .HasColumnName("Message")
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(an => an.OrderId)
                .HasColumnName("OrderId")
                .IsRequired(false);

            builder.Property(an => an.NotificationType)
                .HasColumnName("NotificationType")
                .IsRequired();

            builder.Property(an => an.IsRead)
                .HasColumnName("IsRead")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(an => an.ReadAt)
                .HasColumnName("ReadAt")
                .IsRequired(false);

            builder.Property(an => an.ReadByUserId)
                .HasColumnName("ReadByUserId")
                .IsRequired(false);

            // Audit properties
            builder.Property(an => an.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(an => an.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(an => an.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(an => an.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Relationships
            builder.HasOne(an => an.Order)
                .WithMany()
                .HasForeignKey(an => an.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(an => an.ReadByUser)
                .WithMany()
                .HasForeignKey(an => an.ReadByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(an => an.OrderId);
            builder.HasIndex(an => an.IsRead);
            builder.HasIndex(an => an.CreatedDate);
            builder.HasIndex(an => an.NotificationType);
        }
    }
}

