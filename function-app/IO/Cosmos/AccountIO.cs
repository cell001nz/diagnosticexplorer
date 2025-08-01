using DiagnosticExplorer.Api.Domain;
using Microsoft.Azure.Cosmos;
using System.Net;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace DiagnosticExplorer.IO.Cosmos;

internal class AccountIO(CosmosClient client) : CosmosIOBase(client, "Account"), IAccountIO
{
    public async Task<Account?> GetAccount(string userId)
    {
        try
        {
            var result = await Container.ReadItemAsync<Account>(userId, new PartitionKey(userId));

            return result.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Account> SaveAccount(Account account)
    {
        ItemResponse<Account> result = await Container.UpsertItemAsync(account, new PartitionKey(account.Id));

        return result.Resource;
    }
}

