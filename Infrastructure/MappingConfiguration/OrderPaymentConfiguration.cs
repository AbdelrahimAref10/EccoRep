using Domain.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
    {
        public void Configure(EntityTypeBuilder<OrderPayment> builder)
        {
            builder.ToTable("VO_OrderPayment");

            // Configure primary key
            builder.HasKey(op => op.Id);

            // Configure properties
            builder.Property(op => op.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(op => op.OrderId)
                .HasColumnName("OrderId")
                .IsRequired();

            builder.Property(op => op.PaymentMethodId)
                .HasColumnName("PaymentMethodId")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(op => op.Total)
                .HasColumnName("Total")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(op => op.State)
                .HasColumnName("State")
                .HasConversion<int>()
                .HasDefaultValue(PaymentState.Pending)
                .IsRequired();

            // Configure audit properties
            builder.Property(op => op.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(op => op.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(op => op.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(op => op.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(op => op.Order)
                .WithMany(o => o.OrderPayments)
                .HasForeignKey(op => op.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Configure indexes
            builder.HasIndex(op => op.OrderId)
                .HasDatabaseName("IX_VO_OrderPayment_OrderId");

            builder.HasIndex(op => op.State)
                .HasDatabaseName("IX_VO_OrderPayment_State");
        }
    }
}

