using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
    {
        public void Configure(EntityTypeBuilder<Zone> builder)
        {
            builder.ToTable("VO_Zone");
            builder.HasKey(z => z.ZoneId);
            builder.Property(z => z.ZoneId).ValueGeneratedOnAdd();
            builder.Property(z => z.ZoneGroupId).IsRequired();
            builder.Property(z => z.Name).HasMaxLength(256).IsRequired();
            builder.Property(z => z.Latitude).IsRequired();
            builder.Property(z => z.Longitude).IsRequired();
            builder.Property(z => z.IsActive).IsRequired();
            builder.Property(z => z.CreatedBy).HasMaxLength(256);
            builder.Property(z => z.LastModifiedBy).HasMaxLength(256);

            builder.HasOne(z => z.ZoneGroup)
                .WithMany(g => g.Zones)
                .HasForeignKey(z => z.ZoneGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(z => new { z.ZoneGroupId, z.Name })
                .IsUnique()
                .HasDatabaseName("IX_VO_Zone_Group_Name");
        }
    }
}
