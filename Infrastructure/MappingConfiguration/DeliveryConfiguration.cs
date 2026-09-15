using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
    {
        public void Configure(EntityTypeBuilder<Delivery> builder)
        {
            builder.ToTable("VO_Delivery");

            builder.HasKey(d => d.DeliveryId);

            builder.Property(d => d.DeliveryId)
                .HasColumnName("DeliveryId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(d => d.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(d => d.CityId)
                .HasColumnName("CityId")
                .IsRequired();

            builder.Property(d => d.ZoneId)
                .HasColumnName("ZoneId")
                .IsRequired();

            builder.Property(d => d.FullName)
                .HasColumnName("FullName")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(d => d.MobileNumber)
                .HasColumnName("MobileNumber")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(d => d.Email)
                .HasColumnName("Email")
                .HasMaxLength(256);

            builder.Property(d => d.PersonalImage)
                .HasColumnName("PersonalImage");

            builder.Property(d => d.InvitationCode)
                .HasColumnName("InvitationCode")
                .HasMaxLength(10);

            builder.Property(d => d.InvitationCodeExpiry)
                .HasColumnName("InvitationCodeExpiry");

            builder.Property(d => d.IsInvitationCodeUsed)
                .HasColumnName("IsInvitationCodeUsed")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(d => d.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(d => d.IsDeleted)
                .HasColumnName("IsDeleted")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(d => d.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(d => d.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(d => d.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(d => d.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            builder.HasOne(d => d.User)
                .WithOne(u => u.Delivery)
                .HasForeignKey<Delivery>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.City)
                .WithMany()
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Zone)
                .WithMany()
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Delivery_UserId");

            builder.HasIndex(d => d.CityId)
                .HasDatabaseName("IX_Delivery_CityId");

            builder.HasIndex(d => d.MobileNumber)
                .HasDatabaseName("IX_Delivery_MobileNumber");
        }
    }
}
