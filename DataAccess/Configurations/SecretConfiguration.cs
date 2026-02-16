using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiagnosticExplorer.DataAccess.Configurations;

public class SecretConfiguration : IEntityTypeConfiguration<SecretEntity>
{
    public void Configure(EntityTypeBuilder<SecretEntity> builder)
    {
        builder.ToTable("secrets");
        builder.HasIndex(s => s.SiteId);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Hash)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(s => s.Site)
            .WithMany(site => site.Secrets)
            .HasForeignKey(s => s.SiteId)
            .HasConstraintName("FK_Secret_Site")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

