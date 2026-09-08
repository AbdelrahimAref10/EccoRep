using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class OrderTotalsConfiguration : IEntityTypeConfiguration<OrderTotals>
    {
        public void Configure(EntityTypeBuilder<OrderTotals> builder)
        {
            builder.ToTable("OrderTotals");

            builder.HasKey(ot => ot.Id);

            builder.Property(ot => ot.Id)
                .ValueGeneratedOnAdd();

            builder.Property(ot => ot.OrderId)
                .IsRequired();

            builder.Property(ot => ot.SubTotal)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ot => ot.ServiceFees)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ot => ot.DeliveryFees)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ot => ot.UrgentFees)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ot => ot.TieredDiscount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ot => ot.TotalAfterAllFees)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Relationship with Order (one-to-one)
            builder.HasOne(ot => ot.Order)
                .WithOne()
                .HasForeignKey<OrderTotals>(ot => ot.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Index on OrderId
            builder.HasIndex(ot => ot.OrderId)
                .IsUnique();
        }
    }
}
