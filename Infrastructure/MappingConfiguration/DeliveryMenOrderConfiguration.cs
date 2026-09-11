using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class DeliveryMenOrderConfiguration : IEntityTypeConfiguration<DeliveryMenOrder>
    {
        public void Configure(EntityTypeBuilder<DeliveryMenOrder> builder)
        {
            builder.ToTable("VO_DeliveryMenOrder");

            builder.HasKey(x => x.DeliveryMenOrderId);

            builder.Property(x => x.DeliveryMenOrderId)
                .HasColumnName("DeliveryMenOrderId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(x => x.OrderId).HasColumnName("OrderId").IsRequired();
            builder.Property(x => x.VehicleId).HasColumnName("VehicleId").IsRequired();
            builder.Property(x => x.DeliveryId).HasColumnName("DeliveryId").IsRequired();
            builder.Property(x => x.DeliveryReceivedFromMerchant).HasColumnName("DeliveryReceivedFromMerchant").IsRequired();
            builder.Property(x => x.ReceivedFromMerchantAt).HasColumnName("ReceivedFromMerchantAt");

            builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(256);
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy").HasMaxLength(256);
            builder.Property(x => x.LastModifiedDate).HasColumnName("LastModifiedDate").IsRequired();

            builder.HasIndex(x => new { x.OrderId, x.VehicleId })
                .IsUnique()
                .HasDatabaseName("IX_VO_DeliveryMenOrder_Order_Vehicle");

            builder.HasOne(x => x.Order)
                .WithMany(o => o.DeliveryMenOrders)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(x => x.Delivery)
                .WithMany()
                .HasForeignKey(x => x.DeliveryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
