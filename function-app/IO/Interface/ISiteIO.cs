using DiagnosticExplorer.Api.Domain;

namespace DiagnosticExplorer.IO;

public interface ISiteIO
{
    Task<Site[]> GetSitesForUser(string userId);
    Task<Site?> GetSiteForUser(string siteId, string userId);
    Task<Site> SaveSite(Site site);
    Task<Site> GetSite(string siteId);
}