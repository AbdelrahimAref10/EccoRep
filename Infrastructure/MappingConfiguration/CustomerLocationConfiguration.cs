using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class CustomerLocationConfiguration : IEntityTypeConfiguration<CustomerLocation>
    {
        public void Configure(EntityTypeBuilder<CustomerLocation> builder)
        {
            builder.ToTable("VO_CustomerLocation");

            // Configure primary key
            builder.HasKey(cl => cl.CustomerLocationId);

            // Configure properties
            builder.Property(cl => cl.CustomerLocationId)
                .HasColumnName("CustomerLocationId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(cl => cl.CustomerId)
                .HasColumnName("CustomerId")
                .IsRequired();

            builder.Property(cl => cl.Longitude)
                .HasColumnName("Longitude")
                .HasColumnType("float")
                .IsRequired();

            builder.Property(cl => cl.Latitude)
                .HasColumnName("Latitude")
                .HasColumnType("float")
                .IsRequired();

            builder.Property(cl => cl.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure one-to-one relationship with Customer
            builder.HasOne(cl => cl.Customer)
                .WithOne(c => c.CustomerLocation)
                .HasForeignKey<CustomerLocation>(cl => cl.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create unique index on CustomerId to ensure one location per customer
            builder.HasIndex(cl => cl.CustomerId)
                .IsUnique()
                .HasDatabaseName("IX_CustomerLocation_CustomerId");
        }
    }
}

