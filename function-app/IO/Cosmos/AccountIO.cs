using DiagnosticExplorer.Api.Domain;
using Microsoft.Azure.Cosmos;
using System.Net;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.Cosmos;

internal class AccountIO(CosmosClient client, ILogger logger) : CosmosIOBase<Account>(client, "Account", logger), IAccountIO
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

