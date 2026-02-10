using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WebClient = DiagnosticExplorer.Api.Domain.WebClient;

namespace DiagnosticExplorer.IO.EFCore;

public class DiagDbContext : DbContext
{
    public const string DATABASE_NAME = "diagnosticexplorer";

    public DiagDbContext(DbContextOptions<DiagDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Site> Sites { get; set; }
    public DbSet<DiagProcess> Processes { get; set; }
    public DbSet<DiagValues> DiagValues { get; set; }
    public DbSet<SystemEvent> SinkEvents { get; set; }
    public DbSet<WebClient> WebClients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToContainer("Account");
            entity.HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
        });

        // Site
        modelBuilder.Entity<Site>(entity =>
        {
            entity.ToContainer("Site");
            entity.HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
            entity.OwnsMany(e => e.Roles);
            entity.OwnsMany(e => e.Secrets);
        });

        // DiagProcess
        modelBuilder.Entity<DiagProcess>(entity =>
        {
            entity.ToContainer("Process");
            entity.HasPartitionKey(e => e.SiteId);
            entity.HasKey(e => e.Id);
        });

        // DiagValues
        modelBuilder.Entity<DiagValues>(entity =>
        {
            entity.ToContainer("Values");
            entity.HasPartitionKey(e => e.SiteId);
            entity.HasKey(e => e.Id);
        });

        // SystemEvent (SinkEvent)
        modelBuilder.Entity<SystemEvent>(entity =>
        {
            entity.ToContainer("SinkEvent");
            entity.HasPartitionKey(e => e.ProcessId);
            entity.HasKey(e => e.Id);
        });

        // WebClient
        modelBuilder.Entity<WebClient>(entity =>
        {
            entity.ToContainer("WebClient");
            entity.HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
        });
    }
}
