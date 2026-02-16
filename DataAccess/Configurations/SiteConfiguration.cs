using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiagnosticExplorer.DataAccess.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<SiteEntity>
{
    public void Configure(EntityTypeBuilder<SiteEntity> builder)
    {
        builder.ToTable("sites");

        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.AccountId);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.HasOne(s => s.Account)
            .WithMany(a => a.Sites)
            .HasForeignKey(a => a.AccountId)
            .HasConstraintName("FK_Site_Account")
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}