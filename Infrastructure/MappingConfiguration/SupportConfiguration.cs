using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class SupportConfiguration : IEntityTypeConfiguration<Support>
    {
        public void Configure(EntityTypeBuilder<Support> builder)
        {
            builder.ToTable("VO_Support");

            builder.HasKey(s => s.SupportId);

            builder.Property(s => s.SupportId)
                .HasColumnName("SupportId")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(s => s.CompanyName)
                .HasColumnName("CompanyName")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(s => s.Address)
                .HasColumnName("Address")
                .HasMaxLength(1000);

            builder.Property(s => s.PhoneNumber)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(50);

            builder.Property(s => s.WhatsAppNumber)
                .HasColumnName("WhatsAppNumber")
                .HasMaxLength(50);

            builder.Property(s => s.Email)
                .HasColumnName("Email")
                .HasMaxLength(256);

            builder.Property(s => s.WebsiteUrl)
                .HasColumnName("WebsiteUrl")
                .HasMaxLength(500);

            builder.Property(s => s.FacebookUrl)
                .HasColumnName("FacebookUrl")
                .HasMaxLength(500);

            builder.Property(s => s.InstagramUrl)
                .HasColumnName("InstagramUrl")
                .HasMaxLength(500);

            builder.Property(s => s.TwitterUrl)
                .HasColumnName("TwitterUrl")
                .HasMaxLength(500);

            builder.Property(s => s.LinkedInUrl)
                .HasColumnName("LinkedInUrl")
                .HasMaxLength(500);

            builder.Property(s => s.TikTokUrl)
                .HasColumnName("TikTokUrl")
                .HasMaxLength(500);

            builder.Property(s => s.YouTubeUrl)
                .HasColumnName("YouTubeUrl")
                .HasMaxLength(500);

            builder.Property(s => s.WorkingHours)
                .HasColumnName("WorkingHours")
                .HasMaxLength(500);

            builder.Property(s => s.Latitude)
                .HasColumnName("Latitude")
                .HasColumnType("decimal(18,8)");

            builder.Property(s => s.Longitude)
                .HasColumnName("Longitude")
                .HasColumnType("decimal(18,8)");

            builder.Property(s => s.AdditionalInfo)
                .HasColumnName("AdditionalInfo")
                .HasMaxLength(2000);

            builder.Property(s => s.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(256);

            builder.Property(s => s.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(s => s.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(256);

            builder.Property(s => s.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();
        }
    }
}
