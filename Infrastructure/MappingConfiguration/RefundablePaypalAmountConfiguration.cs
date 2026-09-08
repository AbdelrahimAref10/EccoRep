using Domain.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class RefundablePaypalAmountConfiguration : IEntityTypeConfiguration<RefundablePaypalAmount>
    {
        public void Configure(EntityTypeBuilder<RefundablePaypalAmount> builder)
        {
            builder.ToTable("VO_RefundablePaypalAmount");

            // Configure primary key
            builder.HasKey(rpa => rpa.Id);

            // Configure properties
            builder.Property(rpa => rpa.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(rpa => rpa.CustomerId)
                .HasColumnName("CustomerId")
                .IsRequired();

            builder.Property(rpa => rpa.OrderId)
                .HasColumnName("OrderId")
                .IsRequired();

            builder.Property(rpa => rpa.OrderTotal)
                .HasColumnName("OrderTotal")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(rpa => rpa.CancellationFees)
                .HasColumnName("CancellationFees")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(rpa => rpa.RefundableAmount)
                .HasColumnName("RefundableAmount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(rpa => rpa.State)
                .HasColumnName("State")
                .HasConversion<int>()
                .HasDefaultValue(RefundState.Pending)
                .IsRequired();

            // Configure audit properties
            builder.Property(rpa => rpa.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(rpa => rpa.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(rpa => rpa.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(rpa => rpa.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Configure relationships
            builder.HasOne(rpa => rpa.Customer)
                .WithMany()
                .HasForeignKey(rpa => rpa.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(rpa => rpa.Order)
                .WithMany()
                .HasForeignKey(rpa => rpa.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Configure indexes
            builder.HasIndex(rpa => rpa.OrderId)
                .HasDatabaseName("IX_VO_RefundablePaypalAmount_OrderId");

            builder.HasIndex(rpa => rpa.CustomerId)
                .HasDatabaseName("IX_VO_RefundablePaypalAmount_CustomerId");

            builder.HasIndex(rpa => rpa.State)
                .HasDatabaseName("IX_VO_RefundablePaypalAmount_State");
        }
    }
}

