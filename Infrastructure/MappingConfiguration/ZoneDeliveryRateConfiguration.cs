using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class ZoneDeliveryRateConfiguration : IEntityTypeConfiguration<ZoneDeliveryRate>
    {
        public void Configure(EntityTypeBuilder<ZoneDeliveryRate> builder)
        {
            builder.ToTable("VO_ZoneDeliveryRate");
            builder.HasKey(r => r.ZoneDeliveryRateId);
            builder.Property(r => r.ZoneDeliveryRateId).ValueGeneratedOnAdd();
            builder.Property(r => r.ZoneGroupId).IsRequired();
            builder.Property(r => r.FromZoneId).IsRequired();
            builder.Property(r => r.ToZoneId).IsRequired();
            builder.Property(r => r.Fee).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(r => r.CreatedBy).HasMaxLength(256);
            builder.Property(r => r.LastModifiedBy).HasMaxLength(256);

            builder.HasOne(r => r.ZoneGroup)
                .WithMany(g => g.DeliveryRates)
                .HasForeignKey(r => r.ZoneGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.FromZone)
                .WithMany()
                .HasForeignKey(r => r.FromZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ToZone)
                .WithMany()
                .HasForeignKey(r => r.ToZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.FromZoneId, r.ToZoneId })
                .IsUnique()
                .HasDatabaseName("IX_VO_ZoneDeliveryRate_From_To");
        }
    }
}
