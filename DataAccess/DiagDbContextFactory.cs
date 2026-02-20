using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DiagnosticExplorer.DataAccess;

public class DiagDbContextFactory : IDesignTimeDbContextFactory<DiagDbContext>
{
 public DiagDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<DiagDbContextFactory>(optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DiagDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        optionsBuilder.UseNpgsql(connectionString);

        return new DiagDbContext(optionsBuilder.Options);
    }}

