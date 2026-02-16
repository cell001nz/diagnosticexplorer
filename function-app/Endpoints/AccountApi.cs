using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.Endpoints;

public class AccountApi : ApiBase
{

    public AccountApi(ILogger<AccountApi> logger, DiagDbContext context) : base(logger, context)
    {
    }

    [Function("MyAccount")]
    public async Task<IActionResult> MyAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Account/MyAccount")]
        HttpRequest req)
    {
        var account = await GetLoggedInAccount(req);
        return new OkObjectResult(account);
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

        var account = await _context.Accounts.Where(a => a.Username == cp.UserId)
            .FirstOrDefaultAsync();
        
        if (account == null)
        {
            account = _context.Accounts.Add(new AccountEntity()
            {
                Name = cp.UserId,
                IsActive = true,
                Username = cp.UserId,
                CreatedAt = DateTime.UtcNow
            }).Entity;
            await _context.SaveChangesAsync();
        }
        
        return new Account(account.Id, account.Username);
    }
}
