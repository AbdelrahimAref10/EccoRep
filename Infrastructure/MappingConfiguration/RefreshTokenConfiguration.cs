using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("VO_RefreshToken");

            builder.HasKey(rt => rt.RefreshTokenId);

            builder.Property(rt => rt.RefreshTokenId)
                .HasColumnName("RefreshTokenId")
                .ValueGeneratedOnAdd();

            builder.Property(rt => rt.UserId)
                .HasColumnName("UserId")
                .IsRequired();

            builder.Property(rt => rt.Token)
                .HasColumnName("Token")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(rt => rt.ExpiresAt)
                .HasColumnName("ExpiresAt")
                .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                .HasColumnName("IsRevoked")
                .HasDefaultValue(false);

            builder.Property(rt => rt.IsUsed)
                .HasColumnName("IsUsed")
                .HasDefaultValue(false);

            builder.Property(rt => rt.RevokedAt)
                .HasColumnName("RevokedAt");

            builder.Property(rt => rt.ReplacedByToken)
                .HasColumnName("ReplacedByToken")
                .HasMaxLength(500);

            // Audit properties
            builder.Property(rt => rt.CreatedBy)
                .HasColumnName("CreatedBy")
                .HasMaxLength(100);

            builder.Property(rt => rt.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(rt => rt.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(100);

            builder.Property(rt => rt.LastModifiedDate)
                .HasColumnName("LastModifiedDate")
                .IsRequired();

            // Relationship with ApplicationUser
            builder.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            builder.HasIndex(rt => rt.UserId);

            builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.IsUsed });
        }
    }
}

