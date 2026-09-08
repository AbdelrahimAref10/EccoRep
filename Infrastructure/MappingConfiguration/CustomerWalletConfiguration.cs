using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class CustomerWalletConfiguration : IEntityTypeConfiguration<CustomerWallet>
    {
        public void Configure(EntityTypeBuilder<CustomerWallet> builder)
        {
            builder.ToTable("VO_CustomerWallet");

            builder.HasKey(cw => cw.Id);

            builder.Property(cw => cw.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(cw => cw.CustomerId)
                .HasColumnName("CustomerId")
                .IsRequired();

            builder.Property(cw => cw.OrderId)
                .HasColumnName("OrderId")
                .IsRequired(false);

            builder.Property(cw => cw.Withdraw)
                .HasColumnName("Withdraw")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(cw => cw.Deposit)
                .HasColumnName("Deposit")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(cw => cw.Description)
                .HasColumnName("Description")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(cw => cw.Type)
                .HasColumnName("Type")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(cw => cw.State)
                .HasColumnName("State")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(cw => cw.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.HasOne(cw => cw.Customer)
                .WithMany()
                .HasForeignKey(cw => cw.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasIndex(cw => cw.CustomerId)
                .HasDatabaseName("IX_VO_CustomerWallet_CustomerId");

            builder.HasIndex(cw => cw.Type)
                .HasDatabaseName("IX_VO_CustomerWallet_Type");

            builder.HasIndex(cw => cw.OrderId)
                .HasDatabaseName("IX_VO_CustomerWallet_OrderId");

            builder.HasIndex(cw => cw.State)
                .HasDatabaseName("IX_VO_CustomerWallet_State");
        }
    }
}
