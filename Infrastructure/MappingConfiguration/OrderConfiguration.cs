using Domain.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("VO_Order");

            // Configure primary key
            builder.HasKey(o => o.OrderId);

            // Configure properties
            builder.Property(o => o.OrderId)
                .HasColumnName("OrderId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(o => o.OrderCode)
                .HasColumnName("OrderCode")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(o => o.CustomerId)
                .HasColumnName("CustomerId")
                .IsRequired();

            builder.Property(o => o.SubCategoryId)
                .HasColumnName("SubCategoryId")
                .IsRequired();

            builder.Property(o => o.CityId)
                .HasColumnName("CityId")
                .IsRequired();

            builder.Property(o => o.ReservationDateFrom)
                .HasColumnName("ReservationDateFrom")
                .IsRequired();

            builder.Property(o => o.ReservationDateTo)
                .HasColumnName("ReservationDateTo")
                .IsRequired();

            builder.Property(o => o.VehiclesCount)
                .HasColumnName("VehiclesCount")
                .IsRequired();

            builder.Property(o => o.OrderSubTotal)
                .HasColumnName("OrderSubTotal")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(o => o.OrderTotal)
                .HasColumnName("OrderTotal")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(o => o.Notes)
                .HasColumnName("Notes")
                .HasMaxLength(1000);

            builder.Property(o => o.PassportImage)
                .HasColumnName("PassportImage")
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            builder.Property(o => o.HotelName)
                .HasColumnName("HotelName")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(o => o.HotelAddress)
                .HasColumnName("HotelAddress")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(o => o.HotelPhone)
                .HasColumnName("HotelPhone")
                .HasMaxLength(20);

            builder.Property(o => o.IsUrgent)
                .HasColumnName("IsUrgent")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(o => o.PaymentMethodId)
                .HasColumnName("PaymentMethodId")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(o => o.OrderState)
                .HasColumnName("OrderState")
                .HasConversion<int>()
                .HasDefaultValue(OrderState.Pending)
                .IsRequired();

            builder.Property(o => o.PreviousDebt)
                .HasColumnName("PreviousDebt")
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(o => o.MoneyRefunded)
                .HasColumnName("MoneyRefunded")
                .IsRequired();

            builder.Property(o => o.ReceiptFaultParty)
                .HasColumnName("ReceiptFaultParty");

            builder.Property(o => o.ReceiptRejectNote)
                .HasColumnName("ReceiptRejectNote")
                .HasMaxLength(1000);

            // Configure audit properties
            builder.Property(o => o.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(o => o.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(o => o.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(o => o.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(o => o.SubCategory)
                .WithMany()
                .HasForeignKey(o => o.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(o => o.City)
                .WithMany()
                .HasForeignKey(o => o.CityId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasMany(o => o.OrderVehicles)
                .WithOne(ov => ov.Order)
                .HasForeignKey(ov => ov.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.OrderPayments)
                .WithOne(op => op.Order)
                .HasForeignKey(op => op.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.ReservedVehiclesPerDays)
                .WithOne(rv => rv.Order)
                .HasForeignKey(rv => rv.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(o => o.OrderCode)
                .HasDatabaseName("IX_VO_Order_OrderCode")
                .IsUnique();

            builder.HasIndex(o => o.CustomerId)
                .HasDatabaseName("IX_VO_Order_CustomerId");

            builder.HasIndex(o => o.OrderState)
                .HasDatabaseName("IX_VO_Order_OrderState");

            builder.HasIndex(o => o.CreatedDate)
                .HasDatabaseName("IX_VO_Order_CreatedDate");
        }
    }
}

