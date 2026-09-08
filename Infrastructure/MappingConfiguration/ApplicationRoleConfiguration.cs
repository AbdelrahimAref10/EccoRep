using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.ToTable("VO_Role");

            // Configure primary key
            builder.HasKey(r => r.Id);

            // Configure properties
            builder.Property(r => r.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(r => r.Name)
                .HasColumnName("Name")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(r => r.NormalizedName)
                .HasColumnName("NormalizedName")
                .HasMaxLength(256);

            builder.Property(r => r.ConcurrencyStamp)
                .HasColumnName("ConcurrencyStamp");

            // Configure audit properties
            builder.Property(r => r.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(r => r.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(r => r.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(r => r.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure index
            builder.HasIndex(r => r.NormalizedName)
                .HasDatabaseName("RoleNameIndex")
                .IsUnique()
                .HasFilter("[NormalizedName] IS NOT NULL");
        }
    }
}

