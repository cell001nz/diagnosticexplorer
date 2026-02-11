using Azure;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Domain;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.Cosmos;

internal class ProcessIO(CosmosClient client, ILogger logger) : CosmosIOBase<DiagProcess>(client, "Process", logger), IProcessIO
{
    #region GetProcessForConnectionId(Container processClient, string connectionId)

    public async Task<DiagProcess?> GetProcessForConnectionId(string connectionId)
    {
        string queryString = "SELECT * from c where c.connectionId = @connectionId";

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@connectionId", connectionId);

        return await ReadSingle(query,
            () => $"Process for connectionId {connectionId}");
    }

    #endregion 

    #region SetProcessSending
    
    public async Task SetProcessSending(string processId, string siteId, bool isSending)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Replace("/isSending", isSending),
        };
        
        var response = await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            patchOperations);
        
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} SetProcessSending");
    }

    #endregion
    
    #region SetOnline(string processId, string siteId)

    public async Task SetOnline(string processId, string siteId, DateTime date)
    {
        var response = await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/isOnline", true),
                PatchOperation.Replace("/lastOnline", date),
            ]);
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} SetOnline");
    }
    
    #endregion

    #region Task SetConnectionId(string processId, string siteId, string connectionId)
    
    public async Task SetConnectionId(string processId, string siteId, string connectionId)
    {
        var response = await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/connectionId", connectionId),
                PatchOperation.Replace("/isOnline", true),
                PatchOperation.Replace("/lastOnline", DateTime.UtcNow),
            ]);
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} SetConnectionId");
    }
    
    #endregion

    #region SetLastReceived(string processId, string siteId, DateTime date)

    public async Task SetLastReceived(string processId, string siteId, DateTime date)
    {
        var response = await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/isOnline", true),
                PatchOperation.Replace("/lastOnline", date),
                PatchOperation.Replace("/lastReceived", date),
            ]);
        
        Trace.WriteLine($"*** CHARGE ***{response.RequestCharge} SetLastReceived ");
    }
    #endregion

    #region SetOffline(string processId, string siteId)

    public async Task SetOffline(string processId, string siteId)
    {
        var response = await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/isOnline", false),
                PatchOperation.Replace("/isSending", false),
                PatchOperation.Remove("/instanceId"),
                PatchOperation.Remove("/connectionId")
            ]);
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} SetOffline");
    }
    
    #endregion
    
    #region GetProcessesForSite(string siteId)
    
    public Task<DiagProcess[]> GetProcessesForSite(string siteId)
    {
        string queryString = "SELECT * from c where c.siteId = @siteId";

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@siteId", siteId);
        
        return ReadMulti(query, () => $"Processes for site {siteId}");
    }

    #endregion
    
    #region GetCandidateProcesses(string siteId, string processName, string machineName, string userName))
    
    public async Task<DiagProcess[]> GetCandidateProcesses(string siteId, string processName, string machineName, string userName)
    {
        string queryString = $"""
                              SELECT *
                              FROM c
                              WHERE
                                c.siteId = @siteId
                                and c.processName = @processName
                                and c.machineName = @machineName
                                and c.userName = @userName
                              """;

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@siteId", siteId)
            .WithParameter("@processName", processName)
            .WithParameter("@machineName", machineName)
            .WithParameter("@userName", userName);

        return await ReadMulti(query, () => $"Candidate processes");        
    }

    #endregion
    
    #region GetProcess(string processId, string siteId)
    
    public async Task<DiagProcess?> GetProcess(string processId, string siteId)
    {
        try
        {   
            var result = await Container.ReadItemAsync<DiagProcess>(processId, new PartitionKey(siteId));
            Trace.WriteLine($"*** CHARGE *** {result.RequestCharge} GetProcess");
            return result.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    #endregion
    
    #region SaveProcess(DiagProcess process)
    
    public async Task<DiagProcess> SaveProcess(DiagProcess process)
    {
        var result = await Container.UpsertItemAsync(process, new PartitionKey(process.SiteId));
        Trace.WriteLine($"*** CHARGE *** {result.RequestCharge} SaveProcess");
        return result.Resource;
    }

    #endregion

    #region Delete(string processId, string siteId)
    
    public async Task Delete(string processId, string siteId)
    {
        var result = await Container.DeleteItemAsync<DiagProcess>(processId, new PartitionKey(siteId));
        Trace.WriteLine($"*** CHARGE *** {result.RequestCharge} DeleteProcess");
    }

    #endregion
        
    #region SaveWebSub(WebProcSub sub)
    
    public async Task SaveWebSub(WebProcSub sub)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set($"/subscriptions/{sub.WebConnectionId}", sub),
        };
        
        var response = await Container.PatchItemAsync<DiagProcess>(
            sub.ProcessId,
            new PartitionKey(sub.SiteId),
            patchOperations);
        
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} DeleteWebSub");
    }

    #endregion
        
    #region DeleteWebSub(WebProcSub sub)
    
    public async Task DeleteWebSub(WebProcSub sub)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Remove($"/subscriptions/{sub.WebConnectionId}"),
        };
        
        var response = await Container.PatchItemAsync<DiagProcess>(
            sub.ProcessId,
            new PartitionKey(sub.SiteId),
            patchOperations);
        
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} DeleteWebSub");
    }

    #endregion

    #region GetStaleOnlineProcesses(DateTime cutoffTime)
    
    public async Task<DiagProcess[]> GetStaleOnlineProcesses(DateTime cutoffTime)
    {
        string queryString = """
                              SELECT *
                              FROM c
                              WHERE c.isOnline = true AND c.lastOnline < @cutoffTime
                              """;

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@cutoffTime", cutoffTime);

        return await ReadMulti(query, () => "Stale online processes");
    }

    #endregion

}

