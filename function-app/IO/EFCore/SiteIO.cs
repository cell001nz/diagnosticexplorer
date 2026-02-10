using DiagnosticExplorer.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.EFCore;

internal class SiteIO : ISiteIO
{
    private readonly DiagDbContext _context;
    private readonly ILogger _logger;

    public SiteIO(DiagDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Site?> GetSiteForUser(string siteId, string userId)
    {
        return await _context.Sites
            .WithPartitionKey(siteId)
            .Where(s => s.Id == siteId)
            .Where(s => s.Roles != null && s.Roles.Any(r => r.AccountId == userId))
            .FirstOrDefaultAsync();
    }

    public async Task<Site[]> GetSitesForUser(string userId)
    {
        return await _context.Sites
            .Where(s => s.Roles != null && s.Roles.Any(r => r.AccountId == userId))
            .ToArrayAsync();
    }

    public async Task<Site> SaveSite(Site site)
    {
        var existing = await _context.Sites
            .WithPartitionKey(site.Id)
            .FirstOrDefaultAsync(s => s.Id == site.Id);

        if (existing == null)
        {
            _context.Sites.Add(site);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(site);
            existing.Roles = site.Roles;
            existing.Secrets = site.Secrets;
        }

        await _context.SaveChangesAsync();
        return site;
    }

    public async Task<Site> GetSite(string siteId)
    {
        return await _context.Sites
            .WithPartitionKey(siteId)
            .FirstAsync(s => s.Id == siteId);
    }
}

