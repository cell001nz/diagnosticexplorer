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
using DiagnosticExplorer.IO;

namespace DiagnosticExplorer.Api.Triggers;

public class AccountTrigger : TriggerBase
{

    public AccountTrigger(ILogger<AccountTrigger> logger, IDiagIO diagIo) : base(logger, diagIo)
    {
    }

    [Function("LoggedIn")]
    public async Task<IActionResult> RegisterLogin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Account/RegisterLogin")] HttpRequest req)
    {
        Account acct = await GetOrCreateAccountAsync(req);

        return new OkObjectResult($"Login OK for {acct.Name}");
    }

    private async Task<Account> GetOrCreateAccountAsync(HttpRequest req)
    {
        ClientPrincipal cp = GetClientPrincipal(req);

        var account = await DiagIO.Account.GetAccount(cp.UserId);
        if (account == null)
        {
            account = new Account(cp.UserId, cp.UserDetails);
            account = await DiagIO.Account.SaveAccount(account);
        }

        return account;
    }
}
