using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DiagnosticExplorer.DataAccess;

public class DiagDbContextFactory : IDesignTimeDbContextFactory<DiagDbContext>
{
    public DiagDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DiagDbContext>();
        
        // Design-time connection string for migrations
        // Replace with your actual PostgreSQL connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=diagnosticexplorer;Username=postgres;Password=postgres");
        
        return new DiagDbContext(optionsBuilder.Options);
    }
}

