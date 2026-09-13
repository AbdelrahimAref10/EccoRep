using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class OrderVehicleConfiguration : IEntityTypeConfiguration<OrderVehicle>
    {
        public void Configure(EntityTypeBuilder<OrderVehicle> builder)
        {
            builder.ToTable("VO_OrderVehicle");

            // Configure composite primary key
            builder.HasKey(ov => new { ov.OrderId, ov.VehicleId });

            // Configure properties
            builder.Property(ov => ov.OrderId)
                .HasColumnName("OrderId")
                .IsRequired();

            builder.Property(ov => ov.VehicleId)
                .HasColumnName("VehicleId")
                .IsRequired();

            builder.Property(ov => ov.ReceivedFromOwner).HasColumnName("ReceivedFromOwner").IsRequired();
            builder.Property(ov => ov.ReceivedFromOwnerImageUrl).HasColumnName("ReceivedFromOwnerImageUrl").HasMaxLength(1000);
            builder.Property(ov => ov.ReceivedFromOwnerAt).HasColumnName("ReceivedFromOwnerAt");

            builder.Property(ov => ov.DeliveredToCustomer).HasColumnName("DeliveredToCustomer").IsRequired();
            builder.Property(ov => ov.DeliveredToCustomerImageUrl).HasColumnName("DeliveredToCustomerImageUrl").HasMaxLength(1000);
            builder.Property(ov => ov.DeliveredToCustomerAt).HasColumnName("DeliveredToCustomerAt");

            builder.Property(ov => ov.ReceivedFromCustomer).HasColumnName("ReceivedFromCustomer").IsRequired();
            builder.Property(ov => ov.ReceivedFromCustomerImageUrl).HasColumnName("ReceivedFromCustomerImageUrl").HasMaxLength(1000);
            builder.Property(ov => ov.ReceivedFromCustomerAt).HasColumnName("ReceivedFromCustomerAt");

            builder.Property(ov => ov.DeliveredToOwner).HasColumnName("DeliveredToOwner").IsRequired();
            builder.Property(ov => ov.DeliveredToOwnerImageUrl).HasColumnName("DeliveredToOwnerImageUrl").HasMaxLength(1000);
            builder.Property(ov => ov.DeliveredToOwnerAt).HasColumnName("DeliveredToOwnerAt");

            builder.Property(ov => ov.DeliveryFailed).HasColumnName("DeliveryFailed").IsRequired();
            builder.Property(ov => ov.DeliveryFailureReason).HasColumnName("DeliveryFailureReason").HasMaxLength(1000);
            builder.Property(ov => ov.DeliveryFailureFaultParty).HasColumnName("DeliveryFailureFaultParty");
            builder.Property(ov => ov.MerchantResponseStatus)
                .HasColumnName("MerchantResponseStatus")
                .HasDefaultValue(MerchantVehicleResponseStatus.Pending)
                .IsRequired();

            // Configure audit properties
            builder.Property(ov => ov.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(ov => ov.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(ov => ov.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(ov => ov.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(ov => ov.Order)
                .WithMany(o => o.OrderVehicles)
                .HasForeignKey(ov => ov.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(ov => ov.Vehicle)
                .WithMany()
                .HasForeignKey(ov => ov.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}

