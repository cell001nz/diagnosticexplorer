using System.Security.Cryptography;
using DiagnosticExplorer.Api.Security;
using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using DiagnosticExplorer.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DiagnosticExplorer.Endpoints;

public class SiteApi : ApiBase
{
    public SiteApi(ILogger<AccountApi> logger, DiagDbContext context) : base(logger, context)
    {
    }


    #region GetSites => GET /api/Sites

    [Function("GetSites")]
    public async Task<IActionResult> GetSites(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites")] HttpRequest req)
    {
        var account = await GetLoggedInAccount(req);

        Site[] items = _context.Sites.Where(s => s.AccountId == account.Id)
            .Select(row => new Site()
            {
                Id = row.Id,
                Name = row.Name,
                Secrets = row.Secrets.AsQueryable().Select(sr => new Secret()
                {
                    Id = sr.Id,
                    Name = sr.Name,
                    Hash = sr.Hash
                }).ToList()
            }).ToArray();

        return new OkObjectResult(items);
    }

    #endregion

    #region GetSite => GET /api/Sites/{id}

    [Function("GetSite")]
    public async Task<IActionResult> GetSite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{id}")]
        HttpRequest req, int id)
    {
        Account account = await GetLoggedInAccount(req);

        Site site = await GetSite(account, id);
        return new OkObjectResult(site);
    }

    private IQueryable<SiteEntity> GetSecuredSiteQuery(Account account, int id)
    {
        return _context.Sites
            .Where(s => s.Id == id && s.AccountId == account.Id);
    }

    private async Task<Site> GetSite(Account account, int siteId)
    {
        return await GetSecuredSiteQuery(account, siteId)
            .Select(row => new Site()
            {
                Id = row.Id,
                Name = row.Name,
                Code = row.Code,
                Secrets = row.Secrets.AsQueryable().Select(sr => new Secret()
                {
                    Id = sr.Id,
                    Name = sr.Name,
                    Value = sr.Value,
                    Hash = sr.Hash
                }).ToList()
            }).FirstOrDefaultAsync() ?? throw new ApplicationException("Site not found");
    }

    #endregion

    #region InsertSite => POST /api/Sites BODY

    [Function("InsertSite")]
    public async Task<IActionResult> InsertSite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Sites")]
        HttpRequest req,
        [FromBody] Site site)
    {
        try
        {
            Account account = await GetLoggedInAccount(req);
            SiteEntity siteEntity = _context.Sites.Add(new SiteEntity()).Entity;

            MergeSite(siteEntity, site);
            siteEntity.AccountId = account.Id;
            siteEntity.CreatedAt = DateTime.UtcNow;
            
            _logger.LogWarning($"Setting accountId to {account.Id}");
            
            await _context.SaveChangesAsync();

            Site saved = await GetSite(account, siteEntity.Id);

            return new OkObjectResult(saved);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new { error = ex.ToString() }) { StatusCode = 500 };
        }
    }

    private void MergeSite(SiteEntity entity, Site site)
    {
        PasswordHasher hasher = new PasswordHasher();

        entity.Name = site.Name;
        entity.Code = site.Code;
        entity.Secrets.MergeFrom(
            source: site.Secrets ?? [],
            sourceKey: s => s.Id,
            targetKey: t => t.Id,
            mergeAction: (target, source) =>
            {
                target.SiteId = entity.Id;
                target.Name = source.Name;
                if (source.Id == 0)
                {
                    target.Value = "..." + source.Value.Substring(source.Value.Length - 4, 4);
                    target.Hash = hasher.HashSecret(source.Value);
                }
            }
        );
    }
    
    #endregion

    #region NewSecret => GET /api/Secrets/New

    [Function("NewSecret")]
    public IActionResult NewSecret([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Secrets/New")] HttpRequest req)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] data = new byte[32];
        rng.GetBytes(data);

        return new OkObjectResult(Convert.ToBase64String(data));
    }

    #endregion

    #region UpdateSite => PUT /api/Sites/{id} BODY

    [Function("UpdateSite")]
    public async Task<IActionResult> UpdateSite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Sites/{id}")]
        HttpRequest req,
        string id, [FromBody] Site site)
    {
        try
        {
            Account account = await GetLoggedInAccount(req);

            SiteEntity siteEntity = await _context.Sites
                                        .Include(s => s.Secrets)
                                        .Where(s => s.Id == site.Id && s.AccountId == account.Id)
                                        .FirstOrDefaultAsync()
                                    ?? throw new ApplicationException("Site not found");

            MergeSite(siteEntity, site);
            await _context.SaveChangesAsync();
            Site saved = await GetSite(account, siteEntity.Id);

            return new OkObjectResult(saved);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new { error = ex.ToString() }) { StatusCode = 500 };
        }
    }

    #endregion
    
}
