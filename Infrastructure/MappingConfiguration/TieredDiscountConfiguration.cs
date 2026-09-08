using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class TieredDiscountConfiguration : IEntityTypeConfiguration<TieredDiscount>
    {
        public void Configure(EntityTypeBuilder<TieredDiscount> builder)
        {
            builder.ToTable("VO_TieredDiscount");

            // Configure primary key
            builder.HasKey(td => td.Id);

            // Configure properties
            builder.Property(td => td.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(td => td.CityId)
                .HasColumnName("CityId")
                .IsRequired();

            builder.Property(td => td.From)
                .HasColumnName("From")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(td => td.To)
                .HasColumnName("To")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(td => td.Discount)
                .HasColumnName("Discount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Configure audit properties
            builder.Property(td => td.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(td => td.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(td => td.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(td => td.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(td => td.City)
                .WithMany(c => c.TieredDiscounts)
                .HasForeignKey(td => td.CityId)
                .OnDelete(DeleteBehavior.Cascade); // Delete tiered discounts when city is deleted

            // Configure indexes
            builder.HasIndex(td => td.CityId)
                .HasDatabaseName("IX_TieredDiscounts_CityId");
        }
    }
}

