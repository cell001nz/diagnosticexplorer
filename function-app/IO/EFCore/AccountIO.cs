using DiagnosticExplorer.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.EFCore;

internal class AccountIO : IAccountIO
{
    private readonly DiagDbContext _context;
    private readonly ILogger _logger;

    public AccountIO(DiagDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Account?> GetAccount(string userId)
    {
        return await _context.Accounts
            .WithPartitionKey(userId)
            .FirstOrDefaultAsync(a => a.Id == userId);
    }

    public async Task<Account> SaveAccount(Account account)
    {
        var existing = await _context.Accounts
            .WithPartitionKey(account.Id)
            .FirstOrDefaultAsync(a => a.Id == account.Id);

        if (existing == null)
        {
            _context.Accounts.Add(account);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(account);
        }

        await _context.SaveChangesAsync();
        return account;
    }
}

