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

