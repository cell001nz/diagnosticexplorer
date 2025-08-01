using System.Diagnostics;
using DiagnosticExplorer.Domain;
using Microsoft.Azure.Cosmos;

namespace DiagnosticExplorer.IO.Cosmos;

internal class DiagValueIO(CosmosClient client) : CosmosIOBase(client, "Values"), IDiagValueIO
{
    
    #region Save(DiagValue value)
    
    public async Task Save(DiagValues values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));

        try
        {
            var response = await Container.UpsertItemAsync(values, new PartitionKey(values.SiteId));
            Trace.WriteLine($"Request Charge for Values save is {response.RequestCharge}");
            
            if (IsFailure(response)) 
                throw new ApplicationException($"Failed to save DiagValue with Id: {values.Id}, StatusCode: {response.StatusCode}");
        }
        catch (CosmosException ex)
        {
            throw new ApplicationException($"CosmosDB error while saving DiagValue with Id: {values.Id}", ex);
        }
    }
    
    #endregion
    
    #region Get(string processId, string siteId)

    public async Task<DiagValues?> Get(string processId, string siteId)
    {
        if (string.IsNullOrEmpty(processId)) throw new ArgumentNullException(nameof(processId));
        if (string.IsNullOrEmpty(siteId)) throw new ArgumentNullException(nameof(siteId));

        try
        {
            ItemResponse<DiagValues> response = await Container.ReadItemAsync<DiagValues>(processId, new PartitionKey(siteId));
            if (IsFailure(response)) 
                throw new ApplicationException($"Failed to get DiagValue with Id: {processId}, StatusCode: {response.StatusCode}");

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (CosmosException ex)
        {
            throw new ApplicationException($"CosmosDB error while retrieving DiagValue with ProcessId: {processId} and SiteId: {siteId}", ex);
        }
    }

    #endregion
    
    #region DeleteForProcess(string processId, string siteId)

    public async Task DeleteForProcess(string processId, string siteId)
    {
        await Container.DeleteItemAsync<DiagValues>(processId, new PartitionKey(siteId));
    }

    #endregion
    
}