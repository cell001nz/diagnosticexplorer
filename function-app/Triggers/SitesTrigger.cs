using System.Security.Cryptography;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Api.Security;
using DiagnosticExplorer.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DiagnosticExplorer.Api.Triggers;

public class SitesTrigger : TriggerBase
{
    public SitesTrigger(ILogger<AccountTrigger> logger, IDiagIO diagIo) : base(logger, diagIo)
    {
    }


    #region GetSites => GET /api/Sites

    [Function("GetSites")]
    public async Task<IActionResult> GetSites([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites")] HttpRequest req, ILogger log)
    {
        ClientPrincipal cp = GetClientPrincipal(req);

        Site[] items = await DiagIO.Site.GetSitesForUser(cp.UserId);

        return new OkObjectResult(items);
    }

    #endregion

    #region GetSite => GET /api/Sites/{id}

    [Function("GetSite")]
    public async Task<IActionResult> GetSite([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{id}")] HttpRequest req, string id, ILogger log)
    {
        var cp = GetClientPrincipal(req);

        var site = await DiagIO.Site.GetSiteForUser(id, cp.UserId);

        return new OkObjectResult(site);
    }

    #endregion

    #region InsertSite => POST /api/Sites BODY

    [Function("InsertSite")]
    public async Task<IActionResult> InsertSite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Sites")] HttpRequest req, 
        [FromBody] Site site, 
        ILogger log)
    {
        try
        {
            var cp = GetClientPrincipal(req);

            // if (!string.IsNullOrWhiteSpace(site.Id))
                // return new BadRequestObjectResult("Can't put when Id is specified");

            if (string.IsNullOrWhiteSpace(site.Id))
                site.Id = Guid.NewGuid().ToString();
            
            site.Roles ??= [];
            site.Roles.Add(new SiteRole
            {
                AccountId = cp.UserId,
                Role = SiteRoleType.Admin
            });

            ProcessSecrets(site);

            Site saved = await DiagIO.Site.SaveSite(site);
            
            return new OkObjectResult(saved);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new { error = ex.ToString() }) { StatusCode = 500 };
        }
    }

    #endregion

    #region NewSecret => GET /api/Secrets/New

    [Function("NewSecret")]
    public IActionResult NewSecret([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Secrets/New")] HttpRequest req, ILogger log)
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Sites/{id}")] HttpRequest req, 
        string id, [FromBody] Site site, 
        ILogger log)
    {
        try
        {
            var cp = GetClientPrincipal(req);
            Site? existing = await DiagIO.Site.GetSiteForUser(id, cp.UserId);
            if (existing == null)
                return new NotFoundObjectResult($"Site with id {id} not found");

            if (true != existing.Roles?.Any(r => r.AccountId == cp.UserId && r.Role == SiteRoleType.Admin))
                return new ForbidResult($"Edit Site with id {id} forbidden");

            ProcessSecrets(site, existing);

            var response = await DiagIO.Site.SaveSite(site);

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new { error = ex.ToString() }) { StatusCode = 500 };
        }
    }

    #endregion

   private void ProcessSecrets(Site site, Site? existing = null)
    {
        
        foreach (var secret in site.Secrets ?? [])
        {
            Secret? existingSecret = existing?.Secrets?.FirstOrDefault(s => s.Id == secret.Id);
            if (existingSecret != null)
            {
                //Keep exsting hash and value for existing secrets
                secret.Hash = existingSecret.Hash;
                secret.Value = existingSecret.Value;
            }


            if (string.IsNullOrWhiteSpace(secret.Id))
                secret.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(secret.Hash))
            {
                PasswordHasher hasher = new PasswordHasher();
                secret.Hash = hasher.HashSecret(secret.Value);
                secret.Value = secret.Value.Substring(secret.Value.Length - 4, 4);
            }
        }
    }
}