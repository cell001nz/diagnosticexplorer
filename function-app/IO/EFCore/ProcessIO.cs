using DiagnosticExplorer.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.EFCore;

internal class ProcessIO : IProcessIO
{
    private readonly DiagDbContext _context;
    private readonly ILogger _logger;

    public ProcessIO(DiagDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DiagProcess?> GetProcessForConnectionId(string connectionId)
    {
        return await _context.Processes
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId);
    }

    public async Task SetProcessSending(string processId, string siteId, bool isSending)
    {
        var process = await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process != null)
        {
            process.IsSending = isSending;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetOnline(string processId, string siteId, DateTime date)
    {
        var process = await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process != null)
        {
            process.IsOnline = true;
            process.LastOnline = date;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetConnectionId(string processId, string siteId, string connectionId)
    {
        var process = await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process != null)
        {
            process.ConnectionId = connectionId;
            process.IsOnline = true;
            process.LastOnline = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetLastReceived(string processId, string siteId, DateTime date)
    {
        var process = await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process != null)
        {
            process.IsOnline = true;
            process.LastOnline = date;
            process.LastReceived = date;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetOffline(string processId, string siteId)
    {
        var process = await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process != null)
        {
            process.IsOnline = false;
            process.IsSending = false;
            process.InstanceId = null;
            process.ConnectionId = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DiagProcess[]> GetProcessesForSite(string siteId)
    {
        return await _context.Processes
            .WithPartitionKey(siteId)
            .Where(p => p.SiteId == siteId)
            .ToArrayAsync();
    }

    public async Task<DiagProcess[]> GetCandidateProcesses(string siteId, string processName, string machineName, string userName)
    {
        return await _context.Processes
            .WithPartitionKey(siteId)
            .Where(p => p.SiteId == siteId)
            .Where(p => p.ProcessName == processName)
            .Where(p => p.MachineName == machineName)
            .Where(p => p.UserName == userName)
            .ToArrayAsync();
    }

    public async Task<DiagProcess?> GetProcess(string processId, string siteId)
    {
        return await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);
    }

    public async Task<DiagProcess> SaveProcess(DiagProcess process)
    {
        var existing = await _context.Processes
            .WithPartitionKey(process.SiteId)
            .FirstOrDefaultAsync(p => p.Id == process.Id);

        if (existing == null)
        {
            _context.Processes.Add(process);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(process);
            existing.Subscriptions = process.Subscriptions;
        }

        await _context.SaveChangesAsync();
        return process;
    }

    public async Task Delete(string processId, string siteId)
    {
        var process = await _context.Processes
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process != null)
        {
            _context.Processes.Remove(process);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveWebSub(WebProcSub sub)
    {
        var process = await _context.Processes
            .WithPartitionKey(sub.SiteId)
            .FirstOrDefaultAsync(p => p.Id == sub.ProcessId);

        if (process != null)
        {
            process.Subscriptions[sub.WebConnectionId] = sub;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteWebSub(WebProcSub sub)
    {
        var process = await _context.Processes
            .WithPartitionKey(sub.SiteId)
            .FirstOrDefaultAsync(p => p.Id == sub.ProcessId);

        if (process != null)
        {
            process.Subscriptions.Remove(sub.WebConnectionId);
            await _context.SaveChangesAsync();
        }
    }
}

