using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class OrderJournalConfiguration : IEntityTypeConfiguration<OrderJournal>
    {
        public void Configure(EntityTypeBuilder<OrderJournal> builder)
        {
            builder.ToTable("VO_OrderJournal");

            builder.HasKey(x => x.OrderJournalId);

            builder.Property(x => x.OrderJournalId)
                .HasColumnName("OrderJournalId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(x => x.OrderId).HasColumnName("OrderId");
            builder.Property(x => x.VehicleId).HasColumnName("VehicleId");
            builder.Property(x => x.PartyType).HasColumnName("PartyType").IsRequired();
            builder.Property(x => x.PartyId).HasColumnName("PartyId");
            builder.Property(x => x.Direction).HasColumnName("Direction").IsRequired();
            builder.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(x => x.EntryKind).HasColumnName("EntryKind").IsRequired();
            builder.Property(x => x.IdempotencyKey).HasColumnName("IdempotencyKey").HasMaxLength(200).IsRequired();
            builder.Property(x => x.FaultParty).HasColumnName("FaultParty");
            builder.Property(x => x.Note).HasColumnName("Note").HasMaxLength(1000);

            builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(256);
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy").HasMaxLength(256);
            builder.Property(x => x.LastModifiedDate).HasColumnName("LastModifiedDate").IsRequired();

            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("IX_VO_OrderJournal_IdempotencyKey");

            builder.HasIndex(x => new { x.OrderId, x.PartyType, x.PartyId })
                .HasDatabaseName("IX_VO_OrderJournal_Order_Party");

            builder.HasIndex(x => new { x.PartyType, x.PartyId, x.CreatedDate })
                .HasDatabaseName("IX_VO_OrderJournal_Party_Created");

            builder.HasIndex(x => new { x.OrderId, x.VehicleId })
                .HasDatabaseName("IX_VO_OrderJournal_Order_Vehicle");

            builder.HasOne(x => x.Order)
                .WithMany(o => o.OrderJournals)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
