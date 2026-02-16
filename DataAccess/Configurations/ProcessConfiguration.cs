using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiagnosticExplorer.DataAccess.Configurations;

public class ProcessConfiguration : IEntityTypeConfiguration<ProcessEntity>
{
    public void Configure(EntityTypeBuilder<ProcessEntity> builder)
    {
        builder.ToTable("processes");

        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.SiteId);
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.Property(p => p.UserName).HasMaxLength(50);
        builder.Property(p => p.MachineName).HasMaxLength(50);
        builder.Property(p => p.InstanceId).HasMaxLength(50);
        builder.Property(p => p.ConnectionId).HasMaxLength(50);

        builder.HasOne(p => p.Site)
            .WithMany(a => a.Processes)
            .HasForeignKey(p => p.SiteId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}

