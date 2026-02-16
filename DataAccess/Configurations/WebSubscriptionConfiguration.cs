using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiagnosticExplorer.DataAccess.Configurations;

public class WebSubscriptionConfiguration : IEntityTypeConfiguration<WebSubcriptionEntity>
{
    public void Configure(EntityTypeBuilder<WebSubcriptionEntity> builder)
    {
        builder.ToTable("web_subscriptions");

        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.ProcessId);
        builder.HasIndex(s => s.SessionId);
        builder.HasIndex(s => new { s.SessionId, s.ProcessId }).IsUnique();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.SessionId)
            .HasConstraintName("FK_WebSubscriptions_Session")
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(x => x.Process)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.ProcessId)
            .HasConstraintName("FK_WebSubscriptions_Process")
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}