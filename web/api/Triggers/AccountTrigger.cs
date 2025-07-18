using DiagnosticExplorer.Api;
using DiagnosticExplorer.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text;
using System.Text.Json;

namespace api.Triggers;

public class AccountTrigger : TriggerBase
{

    public AccountTrigger(ILogger<AccountTrigger> logger, CosmosClient client) : base(logger, client)
    {
    }

    [Function("LoggedIn")]
    public async Task<IActionResult> RegisterLogin([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Account/RegisterLogin")] HttpRequest req)
    {
        Account acct = await GetOrCreateAccountAsync(req);

        return new OkObjectResult($"Login OK for {acct.Name}");
    }

    private async Task<Account> GetOrCreateAccountAsync(HttpRequest req)
    {
        ClientPrincipal cp = GetClientPrincipal(req);
        var accounts = _cosmosClient.GetContainer(DIAGNOSTIC_EXPLORER, "Account");

        try
        {
            var found = await accounts.ReadItemAsync<Account>(cp.UserId, new PartitionKey(cp.UserId), new ItemRequestOptions() { });
            return found.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var created = await accounts.CreateItemAsync(new Account(cp.UserId, cp.UserDetails));
            return created.Resource;
        }
    }
}
