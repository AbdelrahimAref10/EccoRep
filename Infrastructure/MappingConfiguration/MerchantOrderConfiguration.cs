using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class MerchantOrderConfiguration : IEntityTypeConfiguration<MerchantOrder>
    {
        public void Configure(EntityTypeBuilder<MerchantOrder> builder)
        {
            builder.ToTable("VO_MerchantOrder");

            builder.HasKey(x => x.MerchantOrderId);

            builder.Property(x => x.MerchantOrderId)
                .HasColumnName("MerchantOrderId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(x => x.OrderId).HasColumnName("OrderId").IsRequired();
            builder.Property(x => x.MerchantId).HasColumnName("MerchantId").IsRequired();
            builder.Property(x => x.ResponseStatus).HasColumnName("ResponseStatus").IsRequired();
            builder.Property(x => x.RejectReason).HasColumnName("RejectReason").HasMaxLength(500);
            builder.Property(x => x.RespondedAt).HasColumnName("RespondedAt");

            builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(256);
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy").HasMaxLength(256);
            builder.Property(x => x.LastModifiedDate).HasColumnName("LastModifiedDate").IsRequired();

            builder.HasIndex(x => new { x.OrderId, x.MerchantId })
                .IsUnique()
                .HasDatabaseName("IX_VO_MerchantOrder_Order_Merchant");

            builder.HasOne(x => x.Order)
                .WithMany(o => o.MerchantOrders)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(x => x.Merchant)
                .WithMany()
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
