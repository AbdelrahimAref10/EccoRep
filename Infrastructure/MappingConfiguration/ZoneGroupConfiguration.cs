using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class ZoneGroupConfiguration : IEntityTypeConfiguration<ZoneGroup>
    {
        public void Configure(EntityTypeBuilder<ZoneGroup> builder)
        {
            builder.ToTable("VO_ZoneGroup");
            builder.HasKey(g => g.ZoneGroupId);
            builder.Property(g => g.ZoneGroupId).ValueGeneratedOnAdd();
            builder.Property(g => g.Name).HasMaxLength(256).IsRequired();
            builder.Property(g => g.IsActive).IsRequired();
            builder.Property(g => g.CreatedBy).HasMaxLength(256);
            builder.Property(g => g.LastModifiedBy).HasMaxLength(256);
            builder.HasIndex(g => g.Name).IsUnique().HasDatabaseName("IX_VO_ZoneGroup_Name");
        }
    }
}
