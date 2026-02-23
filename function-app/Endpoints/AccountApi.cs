using System.Security.Cryptography;
using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.DataExtensions;
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

    [Function("Status")]
    public async Task<IActionResult> Status(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Account/Status")]
        HttpRequest req)
    {
        string info = $"Status: ";
        
        if (req.Headers.TryGetValue("x-ms-client-principal", out var header))
            info += $" UserId = {header.FirstOrDefault()?.Substring(0, 10)}...";
        else
            info += " No user principal header";

        try
        {
            int count = await _context.Sites.CountAsync();
            info += $", Site count = {count}";
        }
        catch (Exception e)
        {
            info += $", Failed to access database: {e.Message}";
        }

        return new OkObjectResult(info);
    }

    [Function("LoggedIn")]
    public async Task<IActionResult> RegisterLogin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Account/RegisterLogin")] HttpRequest req)
    {
        Account acct = await GetOrCreateAccountAsync(req);

        return new OkObjectResult(acct);
    }

    [Function("UpdateProfile")]
    public async Task<IActionResult> UpdateProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Account/UpdateProfile")] HttpRequest req)
    {
        ClientPrincipal cp = GetClientPrincipal(req);

        var profileUpdate = await req.ReadFromJsonAsync<UpdateProfileRequest>();
        if (profileUpdate == null || string.IsNullOrWhiteSpace(profileUpdate.Name) || string.IsNullOrWhiteSpace(profileUpdate.Email))
            return new BadRequestObjectResult("Name and email are required.");

        var account = await _context.Accounts.Where(a => a.Username == cp.UserId)
            .FirstOrDefaultAsync();

        if (account == null)
            return new NotFoundObjectResult("Account not found.");

        account.Name = profileUpdate.Name.Trim();
        account.Email = profileUpdate.Email.Trim();
        account.IsProfileComplete = true;

        await _context.SaveChangesAsync();

        return new OkObjectResult(account.ToDto());
    }

    private async Task<Account> GetOrCreateAccountAsync(HttpRequest req)
    {
        ClientPrincipal cp = GetClientPrincipal(req);

        var account = await _context.Accounts.Where(a => a.Username == cp.UserId)
            .FirstOrDefaultAsync();
        
        if (account == null)
        {
            // Extract email from SWA userDetails (contains email for Google, UPN for AAD)
            string? initialEmail = !string.IsNullOrWhiteSpace(cp.UserDetails) ? cp.UserDetails : null;

            account = _context.Accounts.Add(new AccountEntity()
            {
                Name = cp.UserDetails ?? cp.UserId,
                IsActive = true,
                Username = cp.UserId,
                Email = initialEmail,
                IsProfileComplete = false,
                CreatedAt = DateTime.UtcNow
            }).Entity;
            await _context.SaveChangesAsync();
        }
        
        return account.ToDto();
    }
}

public class UpdateProfileRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

