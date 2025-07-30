using DiagnosticExplorer.Api.Domain;
using Microsoft.Azure.Cosmos;
using System.Net;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace DiagnosticExplorer.IO.Cosmos;

internal class AccountIO(CosmosClient client) : CosmosIOBase(client, "Account"), IAccountIO
{
    public async Task<Account?> GetAccount(string userId)
    {
        var result = await Container.ReadItemAsync<Account>(userId, new PartitionKey(userId));
        if (result.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        if (IsFailure(result.StatusCode))
            throw new ApplicationException($"Error retrieving account: {result.StatusCode}");

        return result.Resource;
    }

    public async Task<Account> SaveAccount(Account account)
    {
        ItemResponse<Account> result = await Container.UpsertItemAsync(account, new PartitionKey(account.Id));
        
        if (IsFailure(result))
            throw new ApplicationException($"Error saving account: {result.StatusCode}");

        return result.Resource;
    }
}

