using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class MerchantOrderPaymentDetailConfiguration : IEntityTypeConfiguration<MerchantOrderPaymentDetail>
    {
        public void Configure(EntityTypeBuilder<MerchantOrderPaymentDetail> builder)
        {
            builder.ToTable("VO_MerchantOrderPaymentDetail");

            builder.HasKey(x => x.MerchantOrderPaymentDetailId);

            builder.Property(x => x.MerchantOrderPaymentDetailId)
                .HasColumnName("MerchantOrderPaymentDetailId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(x => x.OrderId).HasColumnName("OrderId").IsRequired();
            builder.Property(x => x.MerchantId).HasColumnName("MerchantId").IsRequired();
            builder.Property(x => x.VehicleId).HasColumnName("VehicleId").IsRequired();
            builder.Property(x => x.VehicleRental).HasColumnName("VehicleRental").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.ServiceFeeShare).HasColumnName("ServiceFeeShare").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.NetAmount).HasColumnName("NetAmount").HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(256);
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy").HasMaxLength(256);
            builder.Property(x => x.LastModifiedDate).HasColumnName("LastModifiedDate").IsRequired();

            builder.HasIndex(x => new { x.OrderId, x.VehicleId })
                .IsUnique()
                .HasDatabaseName("IX_VO_MerchantOrderPaymentDetail_Order_Vehicle");

            builder.HasOne(x => x.Order)
                .WithMany(o => o.MerchantOrderPaymentDetails)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
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
