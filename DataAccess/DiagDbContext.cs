using DiagnosticExplorer.DataAccess.Configurations;
using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiagnosticExplorer.DataAccess;

public class DiagDbContext : DbContext
{
    public DiagDbContext(DbContextOptions<DiagDbContext> options)
        : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        
        // Apply snake_case naming convention for PostgreSQL
        configurationBuilder.Properties<string>().HaveMaxLength(1000);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply snake_case naming convention to all tables and columns
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table names to snake_case
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.DisplayName()));

            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Convert primary key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? $"pk_{entity.GetTableName()}"));
            }

            // Convert foreign key names to snake_case
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName() ?? 
                    $"fk_{entity.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}"));
            }

            // Convert index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? 
                    $"ix_{entity.GetTableName()}_{string.Join("_", index.Properties.Select(p => p.Name))}"));
            }
        }

        modelBuilder.ApplyConfiguration(new SiteConfiguration());
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessConfiguration());
        modelBuilder.ApplyConfiguration(new WebSessionConfiguration());
        modelBuilder.ApplyConfiguration(new WebSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new SecretConfiguration());
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return System.Text.RegularExpressions.Regex
            .Replace(name, "(?<!^)([A-Z])", "_$1")
            .ToLowerInvariant();
    }

    public DbSet<SiteEntity> Sites => Set<SiteEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<ProcessEntity> Processes => Set<ProcessEntity>();
    public DbSet<WebSessionEntity> WebSessions => Set<WebSessionEntity>();
    public DbSet<WebSubcriptionEntity> WebSubscriptions => Set<WebSubcriptionEntity>();
    public DbSet<SecretEntity> Secrets => Set<SecretEntity>();
}