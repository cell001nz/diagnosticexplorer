using System.Collections;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using DiagnosticExplorer.Api;
using DiagnosticExplorer.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace api.Triggers;

public class SitesTrigger : TriggerBase
{

    public SitesTrigger(ILogger<AccountTrigger> logger, CosmosClient client) : base(logger, client)
    {
    }

    #region GetSites => GET /api/Sites
    
    [Function("GetSites")]
    public async Task<IActionResult> GetSites([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites")] HttpRequest req, ILogger log)
    {
        var cp = GetClientPrincipal(req);

        var siteCtnr = _cosmosClient.GetContainer(DIAGNOSTIC_EXPLORER, "Site");

        var siteQueryable = siteCtnr
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

    private static Site? GetSite(Container siteCtnr, ClientPrincipal cp, string id)
    {
        var siteQueryable = siteCtnr
            .GetItemLinqQueryable<Site>(allowSynchronousQueryExecution: true);

        var result = (from site in siteQueryable
                where site.Id == id && site.Roles.Any(r => r.AccountId == cp.UserId)
                select site
            ).FirstOrDefault();
        return result;
    }


}
