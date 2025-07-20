using System.Collections;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiagnosticExplorer.Api;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DiagnosticExplorer.Api.Triggers;

public class SitesTrigger : TriggerBase
{

    public SitesTrigger(ILogger<AccountTrigger> logger, CosmosClient client) : base(logger, client)
    {
    }

    #region GetSites => GET /api/Sites
    
    [Function("Hello")]
    public async Task<IActionResult> Hello([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Hello")] HttpRequest req, ILogger log)
    {
        
        return new OkObjectResult($"Hello {DateTime.Now}");
    }

    #endregion

    #region GetSites => GET /api/Sites
    
    [Function("GetSites")]
    public async Task<IActionResult> GetSites([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites")] HttpRequest req, ILogger log)
    {
        var cp = GetClientPrincipal(req);

        var siteClient = _cosmosClient.GetContainer(DIAGNOSTIC_EXPLORER, "Site");

        var siteQueryable = siteClient
            .GetItemLinqQueryable<Site>(allowSynchronousQueryExecution: true);

        Site[] result = (from site in siteQueryable
                where site.Roles.Any(r => r.AccountId == cp.UserId)
                select site
            ).ToArray();

        return new OkObjectResult(result);
    }

    #endregion

    #region GetSite => GET /api/Sites/{id}

    [Function("GetSite")]
    public async Task<IActionResult> GetSite([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{id}")] HttpRequest req, string id, ILogger log)
    {
        var cp = GetClientPrincipal(req);
        
        var siteClient = _cosmosClient.GetContainer(DIAGNOSTIC_EXPLORER, "Site");

        var site = GetSite(siteClient, cp, id);

        return new OkObjectResult(site);
    }

    #endregion

    #region InsertSite => POST /api/Sites BODY
    
    [Function("InsertSite")]
    public async Task<IActionResult> InsertSite([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Sites")] HttpRequest req, [FromBody] Site site, ILogger log)
    {
        try
        {
            var cp = GetClientPrincipal(req);

            if (!string.IsNullOrWhiteSpace(site.Id))
                return new BadRequestObjectResult("Can't put when Id is specified");

            site.Id = Guid.NewGuid().ToString();
            site.Roles.Add(new SiteRole
            {
                AccountId = cp.UserId,
                Role = SiteRoleType.Admin
            });

            ProcessSecrets(site);

            var siteClient = _cosmosClient.GetContainer(DIAGNOSTIC_EXPLORER, "Site");
            var response = await siteClient.UpsertItemAsync(site);

            return new OkObjectResult(response.Resource);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new { error = ex.ToString() }) { StatusCode = 500 };
        }
    }


    #endregion

    #region NewSecret => Get /api/Secrets/New

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
    public async Task<IActionResult> UpdateSite([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Sites/{id}")] HttpRequest req, string id, [FromBody] Site site, ILogger log)
    {
        try
        {
            var cp = GetClientPrincipal(req);
            var siteClient = _cosmosClient.GetContainer(DIAGNOSTIC_EXPLORER, "Site");
            var existing = GetSite(siteClient, cp, id);
            if (existing == null)
                return new NotFoundObjectResult($"Site with id {id} not found");

            if (!existing.Roles.Any(r => r.AccountId == cp.UserId && r.Role == SiteRoleType.Admin))
                return new ForbidResult($"Edit Site with id {id} forbidden");

            ProcessSecrets(site, existing);

            var response = await siteClient.UpsertItemAsync(site);

            return new OkObjectResult(response.Resource);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObjectResult(new { error = ex.ToString() }) { StatusCode = 500 };
        }
    }

    #endregion

    private static Site? GetSite(Container siteClient, ClientPrincipal cp, string id)
    {
        var siteQueryable = siteClient.GetItemLinqQueryable<Site>(allowSynchronousQueryExecution: true);

        var result = (from site in siteQueryable
                where site.Id == id && site.Roles.Any(r => r.AccountId == cp.UserId)
                select site
            ).FirstOrDefault();
        return result;
    }

    private void ProcessSecrets(Site site, Site? existing = null)
    {
        foreach (var secret in site.Secrets ?? [])
        {
            Secret? existingSecret = existing?.Secrets.FirstOrDefault(s => s.Id == secret.Id);
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
