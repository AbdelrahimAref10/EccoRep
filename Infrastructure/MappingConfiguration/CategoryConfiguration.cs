using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("VO_Category");

            // Configure primary key
            builder.HasKey(c => c.CategoryId);

            // Configure properties
            builder.Property(c => c.CategoryId)
                .HasColumnName("CategoryId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(c => c.Name)
                .HasColumnName("Name")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasColumnName("Description")
                .HasMaxLength(1000);

            builder.Property(c => c.ImageUrl)
                .HasColumnName("ImageUrl")
                .HasColumnType("nvarchar(max)");

            builder.Property(c => c.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(c => c.CityId)
                .HasColumnName("CityId")
                .IsRequired();

            // Configure audit properties
            builder.Property(c => c.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(c => c.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(c => c.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(c => c.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(c => c.City)
                .WithMany()
                .HasForeignKey(c => c.CityId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to preserve data integrity

            // Configure indexes
            builder.HasIndex(c => c.CityId)
                .HasDatabaseName("IX_VO_Category_CityId");

            // Configure indexes
            builder.HasIndex(c => c.Name)
                .HasDatabaseName("IX_VO_Category_Name")
                .IsUnique();

            builder.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_VO_Category_IsActive");
        }
    }
}

