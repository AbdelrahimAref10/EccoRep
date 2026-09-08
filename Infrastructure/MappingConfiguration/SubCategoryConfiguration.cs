using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class SubCategoryConfiguration : IEntityTypeConfiguration<SubCategory>
    {
        public void Configure(EntityTypeBuilder<SubCategory> builder)
        {
            builder.ToTable("VO_SubCategory");

            // Configure primary key
            builder.HasKey(sc => sc.SubCategoryId);

            // Configure properties
            builder.Property(sc => sc.SubCategoryId)
                .HasColumnName("SubCategoryId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(sc => sc.Name)
                .HasColumnName("Name")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(sc => sc.Description)
                .HasColumnName("Description")
                .HasMaxLength(1000);

            builder.Property(sc => sc.ImageUrl)
                .HasColumnName("ImageUrl")
                .HasColumnType("nvarchar(max)");

            builder.Property(sc => sc.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(sc => sc.IsOffer)
                .HasColumnName("IsOffer")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(sc => sc.Price)
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(sc => sc.CategoryId)
                .HasColumnName("CategoryId")
                .IsRequired();

            // Configure audit properties
            builder.Property(sc => sc.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(sc => sc.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(sc => sc.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(sc => sc.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(sc => sc.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasMany(sc => sc.Vehicles)
                .WithOne(v => v.SubCategory)
                .HasForeignKey(v => v.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes
            builder.HasIndex(sc => sc.Name)
                .HasDatabaseName("IX_VO_SubCategory_Name")
                .IsUnique();

            builder.HasIndex(sc => sc.IsActive)
                .HasDatabaseName("IX_VO_SubCategory_IsActive");

            builder.HasIndex(sc => sc.CategoryId)
                .HasDatabaseName("IX_VO_SubCategory_CategoryId");

            builder.HasIndex(sc => sc.IsOffer)
                .HasDatabaseName("IX_VO_SubCategory_IsOffer");
        }
    }
}

