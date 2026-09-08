using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("VO_Employee");

            builder.HasKey(e => e.EmployeeId);

            builder.Property(e => e.EmployeeId)
                .HasColumnName("EmployeeId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(e => e.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(e => e.FullName)
                .HasColumnName("FullName")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(e => e.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(e => e.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(e => e.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(e => e.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            builder.HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Employee_UserId");
        }
    }
}
