using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.IO.Cosmos;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.Cosmos;

internal class SiteIO : CosmosIOBase<Site>, ISiteIO
{
    
    public SiteIO(CosmosClient client, ILogger logger) : base(client, "Site", logger)
    {
    }


    #region GetSiteForUser(string siteId, string userId)
    
    public async Task<Site?> GetSiteForUser(string siteId, string userId)
    {
        string queryString = """
                              SELECT TOP 1 *
                              FROM c
                              WHERE c.id = @siteId
                                AND ARRAY_LENGTH(c.roles) > 0
                                AND EXISTS (
                                  SELECT VALUE r
                                  FROM r IN c.roles
                                  WHERE r.accountId = @userId
                                )
                              """;

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@siteId", siteId)
            .WithParameter("@userId", userId);

        return await ReadSingle(query, () => $"Site {siteId} for user {userId}");
    }

    #endregion
    
    #region GetSitesForUser(string userId)
    
    public async Task<Site[]> GetSitesForUser(string userId)
    {
        string queryString = """
                              SELECT *
                              FROM c
                              WHERE ARRAY_LENGTH(c.roles) > 0
                                AND EXISTS (
                                  SELECT VALUE r
                                  FROM r IN c.roles
                                  WHERE r.accountId = @userId
                                )
                              """;

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@userId", userId);

        Trace.WriteLine($"Find sites for {userId}");
        return await ReadMulti(query, () => $"Sites for user {userId}");
    }

    #endregion

    #region SaveSite(Site site)

    public async Task<Site> SaveSite(Site site)
    {
        var response = await Container.UpsertItemAsync(site);
        return response.Resource;
    }

    #endregion
    
    #region GetSite(string siteId)
    
    public async Task<Site> GetSite(string siteId)
    {
        var result = await Container.ReadItemAsync<Site>(siteId, new PartitionKey(siteId));
        return result.Resource;
    }

    #endregion
}

