using Domain.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class ReservedVehiclesPerDaysConfiguration : IEntityTypeConfiguration<ReservedVehiclesPerDays>
    {
        public void Configure(EntityTypeBuilder<ReservedVehiclesPerDays> builder)
        {
            builder.ToTable("VO_ReservedVehiclesPerDays");

            // Configure primary key
            builder.HasKey(rv => rv.Id);

            // Configure properties
            builder.Property(rv => rv.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(rv => rv.VehicleId)
                .HasColumnName("VehicleId")
                .IsRequired();

            builder.Property(rv => rv.SubCategoryId)
                .HasColumnName("SubCategoryId")
                .IsRequired();

            builder.Property(rv => rv.VehicleCode)
                .HasColumnName("VehicleCode")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(rv => rv.OrderId)
                .HasColumnName("OrderId")
                .IsRequired();

            builder.Property(rv => rv.DateFrom)
                .HasColumnName("DateFrom")
                .IsRequired();

            builder.Property(rv => rv.DateTo)
                .HasColumnName("DateTo")
                .IsRequired();

            builder.Property(rv => rv.State)
                .HasColumnName("State")
                .HasConversion<int>()
                .HasDefaultValue(ReservedVehicleState.StillBooked)
                .IsRequired();

            // Configure audit properties
            builder.Property(rv => rv.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(rv => rv.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(rv => rv.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(rv => rv.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(rv => rv.Vehicle)
                .WithMany()
                .HasForeignKey(rv => rv.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(rv => rv.SubCategory)
                .WithMany()
                .HasForeignKey(rv => rv.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(rv => rv.Order)
                .WithMany(o => o.ReservedVehiclesPerDays)
                .HasForeignKey(rv => rv.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Configure indexes
            builder.HasIndex(rv => rv.SubCategoryId)
                .HasDatabaseName("IX_VO_ReservedVehiclesPerDays_SubCategoryId");

            builder.HasIndex(rv => rv.State)
                .HasDatabaseName("IX_VO_ReservedVehiclesPerDays_State");

            builder.HasIndex(rv => new { rv.SubCategoryId, rv.DateFrom, rv.DateTo })
                .HasDatabaseName("IX_VO_ReservedVehiclesPerDays_SubCategoryId_Dates");

            builder.HasIndex(rv => rv.VehicleId)
                .HasDatabaseName("IX_VO_ReservedVehiclesPerDays_VehicleId");
        }
    }
}

