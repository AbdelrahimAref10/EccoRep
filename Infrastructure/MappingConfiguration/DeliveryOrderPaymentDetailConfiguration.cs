using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class DeliveryOrderPaymentDetailConfiguration : IEntityTypeConfiguration<DeliveryOrderPaymentDetail>
    {
        public void Configure(EntityTypeBuilder<DeliveryOrderPaymentDetail> builder)
        {
            builder.ToTable("VO_DeliveryOrderPaymentDetail");

            builder.HasKey(x => x.DeliveryOrderPaymentDetailId);

            builder.Property(x => x.DeliveryOrderPaymentDetailId)
                .HasColumnName("DeliveryOrderPaymentDetailId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(x => x.OrderId).HasColumnName("OrderId").IsRequired();
            builder.Property(x => x.DeliveryId).HasColumnName("DeliveryId").IsRequired();
            builder.Property(x => x.VehicleId).HasColumnName("VehicleId").IsRequired();
            builder.Property(x => x.DeliveryFeeShare).HasColumnName("DeliveryFeeShare").HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(256);
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy").HasMaxLength(256);
            builder.Property(x => x.LastModifiedDate).HasColumnName("LastModifiedDate").IsRequired();

            builder.HasIndex(x => new { x.OrderId, x.VehicleId })
                .IsUnique()
                .HasDatabaseName("IX_VO_DeliveryOrderPaymentDetail_Order_Vehicle");

            builder.HasOne(x => x.Order)
                .WithMany(o => o.DeliveryOrderPaymentDetails)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(x => x.Delivery)
                .WithMany()
                .HasForeignKey(x => x.DeliveryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
