using DiagnosticExplorer.Api.Domain;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Security.Policy;
using WebClient = DiagnosticExplorer.Api.Domain.WebClient;

namespace DiagnosticExplorer.IO.Cosmos;

internal class WebClientIO : CosmosIOBase<WebClient>, IWebClientIO
{
    
    public WebClientIO(CosmosClient client, ILogger logger) : base(client, "WebClient", logger)
    {
    }
   
    #region Get(string connectionId)

    public async Task<WebClient?> Get(string connectionId)
    {
        return await GetItem(connectionId);
    }


    #endregion
    
    #region Save(WebClient client)

    public async Task<WebClient> Save(WebClient client)
    {
        var response = await Container.UpsertItemAsync(client, new PartitionKey(client.Id));
        return response.Resource;
    }
    
    #endregion
    
    #region Delete(string clientId)

    public async Task Delete(string clientId)
    {
        try
        {
            await Container.DeleteItemAsync<WebClient>(clientId, new PartitionKey(clientId));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Logger.LogWarning($"Failed to delete WebClient {clientId}");
        }
    }

    public async Task SaveWebSub(WebProcSub sub)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set($"/subscriptions/{sub.ProcessId}", sub),
        };
        
        var response = await Container.PatchItemAsync<DiagProcess>(
            sub.WebConnectionId,
            new PartitionKey(sub.WebConnectionId),
            patchOperations);
        
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} SaveWebProcSub");
    }

    public async Task DeleteWebSub(WebProcSub sub)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set($"/subscriptions/{sub.ProcessId}", sub),
        };
        
        var response = await Container.PatchItemAsync<DiagProcess>(
            sub.WebConnectionId,
            new PartitionKey(sub.WebConnectionId),
            patchOperations);
        
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} DeleteWebSub");
    }

    #endregion
}

