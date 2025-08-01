using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Domain;
using Microsoft.Azure.Cosmos;

namespace DiagnosticExplorer.IO.Cosmos;

internal class ProcessIO(CosmosClient client) : CosmosIOBase(client, "Process"), IProcessIO
{
    #region GetProcessForConnectionId(Container processClient, string connectionId)

    public async Task<DiagProcess?> GetProcessForConnectionId(string connectionId)
    {
        string queryString = "SELECT * from c where c.connectionId = @connectionId";
        
        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@connectionId", connectionId);

            return await ReadSingle<DiagProcess>(Container, query,
                () => $"Process for connectionId {connectionId}");
    }
    
    #endregion 

    #region SetProcessSending
    
    public Task SetProcessSending(string processId, string siteId, bool isSending)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Replace("/isSending", isSending),
        };
        
        return Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            patchOperations);
    }

    #endregion
    
    #region SetOnline(string processId, string siteId)

    public async Task SetOnline(string processId, string siteId, DateTime date)
    {
        await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/isOnline", true),
                PatchOperation.Replace("/lastOnline", date),
            ]);
    }
    #endregion

    #region SetLastReceived(string processId, string siteId, DateTime date)

    public async Task SetLastReceived(string processId, string siteId, DateTime date)
    {
        await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/isOnline", true),
                PatchOperation.Replace("/lastOnline", date),
                PatchOperation.Replace("/lastReceived", date),
            ]);
    }
    #endregion

    #region SetOffline(string processId, string siteId)

    public async Task SetOffline(string processId, string siteId)
    {
        await Container.PatchItemAsync<DiagProcess>(
            processId,
            new PartitionKey(siteId),
            [
                PatchOperation.Replace("/isOnline", false),
                PatchOperation.Replace("/isSending", false),
                PatchOperation.Remove("/instanceId"),
                PatchOperation.Remove("/connectionId")
            ]);
    }
    #endregion
    
    #region GetProcessesForSite(string siteId)
    
    public Task<DiagProcess[]> GetProcessesForSite(string siteId)
    {
        string queryString = "SELECT * from c where c.siteId = @siteId";

        QueryDefinition query = new QueryDefinition(queryString)
            .WithParameter("@siteId", siteId);
        
        return ReadMulti<DiagProcess>(Container, query, () => $"Processes for site {siteId}");
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

        return await ReadMulti<DiagProcess>(Container, query, () => $"Candidate processes");        
    }

    #endregion
    
    #region GetProcess(string processId, string siteId)
    
    public async Task<DiagProcess?> GetProcess(string processId, string siteId)
    {
        try
        {   
            var result = await Container.ReadItemAsync<DiagProcess>(processId, new PartitionKey(siteId));
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
        return result.Resource;
    }

    #endregion

    #region Delete(string processId, string siteId)
    
    public async Task Delete(string processId, string siteId)
    {
        await Container.DeleteItemAsync<DiagProcess>(processId, new PartitionKey(siteId));
    }

    #endregion
}

