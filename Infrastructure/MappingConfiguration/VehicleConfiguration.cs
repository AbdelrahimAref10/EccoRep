using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.ToTable("VO_Vehicle");

            builder.HasKey(v => v.VehicleId);

            builder.Property(v => v.VehicleId)
                .HasColumnName("VehicleId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(v => v.Name)
                .HasColumnName("Name")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(v => v.VehicleCode)
                .HasColumnName("VehicleCode")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(v => v.ImageUrl)
                .HasColumnName("ImageUrl")
                .HasColumnType("nvarchar(max)");

            // DB stores enum name as nvarchar (Available / UnderMaintenance / Rented)
            builder.Property(v => v.Status)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(v => v.SubCategoryId)
                .HasColumnName("SubCategoryId")
                .IsRequired();

            builder.Property(v => v.MerchantId)
                .HasColumnName("MerchantId")
                .IsRequired();

            builder.Property(v => v.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(v => v.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(v => v.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(v => v.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            builder.HasOne(v => v.SubCategory)
                .WithMany(sc => sc.Vehicles)
                .HasForeignKey(v => v.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(v => v.Merchant)
                .WithMany(m => m.Vehicles)
                .HasForeignKey(v => v.MerchantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasIndex(v => v.SubCategoryId)
                .HasDatabaseName("IX_VO_Vehicle_SubCategoryId");

            builder.HasIndex(v => v.MerchantId)
                .HasDatabaseName("IX_VO_Vehicle_MerchantId");

            builder.HasIndex(v => v.Status)
                .HasDatabaseName("IX_VO_Vehicle_Status");

            builder.HasIndex(v => new { v.SubCategoryId, v.Status })
                .HasDatabaseName("IX_VO_Vehicle_SubCategoryId_Status");
        }
    }
}
