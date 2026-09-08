using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("VO_City");

            // Configure primary key
            builder.HasKey(c => c.CityId);

            // Configure properties
            builder.Property(c => c.CityId)
                .HasColumnName("CityId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(c => c.Name)
                .HasColumnName("Name")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasColumnName("Description")
                .HasMaxLength(1000);

            builder.Property(c => c.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(c => c.DeliveryFees)
                .HasColumnName("DeliveryFees")
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.UrgentDelivery)
                .HasColumnName("UrgentDelivery")
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.ServiceFees)
                .HasColumnName("ServiceFees")
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.CancellationFees)
                .HasColumnName("CancellationFees")
                .HasColumnType("decimal(18,2)");

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
            builder.HasMany(c => c.Customers)
                .WithOne(c => c.City)
                .HasForeignKey(c => c.CityId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deletion if City has customers

            // Configure indexes
            builder.HasIndex(c => c.Name)
                .HasDatabaseName("IX_Cities_Name")
                .IsUnique();

            builder.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Cities_IsActive");
        }
    }
}

