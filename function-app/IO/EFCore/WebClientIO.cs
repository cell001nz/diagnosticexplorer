using DiagnosticExplorer.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebClient = DiagnosticExplorer.Api.Domain.WebClient;

namespace DiagnosticExplorer.IO.EFCore;

internal class WebClientIO : IWebClientIO
{
    private readonly DiagDbContext _context;
    private readonly ILogger _logger;

    public WebClientIO(DiagDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WebClient?> Get(string connectionId)
    {
        return await _context.WebClients
            .WithPartitionKey(connectionId)
            .FirstOrDefaultAsync(c => c.Id == connectionId);
    }

    public async Task<WebClient> Save(WebClient client)
    {
        var existing = await _context.WebClients
            .WithPartitionKey(client.Id)
            .FirstOrDefaultAsync(c => c.Id == client.Id);

        if (existing == null)
        {
            _context.WebClients.Add(client);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(client);
            existing.Subscriptions = client.Subscriptions;
        }

        await _context.SaveChangesAsync();
        return client;
    }

    public async Task Delete(string clientId)
    {
        var client = await _context.WebClients
            .WithPartitionKey(clientId)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client != null)
        {
            _context.WebClients.Remove(client);
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("Failed to delete WebClient {ClientId}", clientId);
        }
    }

    public async Task SaveWebSub(WebProcSub sub)
    {
        var client = await _context.WebClients
            .WithPartitionKey(sub.WebConnectionId)
            .FirstOrDefaultAsync(c => c.Id == sub.WebConnectionId);

        if (client != null)
        {
            client.Subscriptions[sub.ProcessId] = sub;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteWebSub(WebProcSub sub)
    {
        var client = await _context.WebClients
            .WithPartitionKey(sub.WebConnectionId)
            .FirstOrDefaultAsync(c => c.Id == sub.WebConnectionId);

        if (client != null)
        {
            client.Subscriptions.Remove(sub.ProcessId);
            await _context.SaveChangesAsync();
        }
    }
}

