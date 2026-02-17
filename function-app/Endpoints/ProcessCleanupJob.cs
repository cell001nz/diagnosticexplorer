using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.Endpoints;

public class ProcessCleanupJob : ApiBase
{
    public ProcessCleanupJob(ILogger<ProcessCleanupJob> logger, DiagDbContext context) : base(logger, context)
    {
    }

    /*/// <summary>
    /// Timer trigger that runs every minute to mark stale online processes as offline.
    /// Processes are considered stale if they haven't been online for more than 2 minutes.
    /// </summary>
    [Function("ProcessCleanup")]
    public async Task<SignalRMessageAction[]> Run([TimerTrigger("0 #1#1 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("ProcessCleanup timer triggered at: {Time}", DateTime.UtcNow);

        var cutoffTime = DateTime.UtcNow.AddMilliseconds(-1 * PROCESS_STALE_TIME_MILLIS);
        var staleProcesses = await _context.Processes
            .Where(p => p.IsOnline)
            .Where(p => p.LastOnline < cutoffTime)
            .ToArrayAsync();

        _logger.LogInformation("Found {Count} stale online processes", staleProcesses.Length);

        List<SignalRMessageAction> actions = [];

        foreach (var process in staleProcesses)
        {
            if (process.ConnectionId == null)
            {
                actions.Add(new SignalRMessageAction(nameof(IProcessHubClient.StopSending)) { ConnectionId = process.ConnectionId});
            }
            
            process.IsOnline = false;
            process.InstanceId = null;
            process.ConnectionId = null;
            process.IsSending = false;
            try
            {
                
                _logger.LogInformation("Set process {ProcessId} offline (last online: {LastOnline})", 
                    process.Id, process.LastOnline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set process {ProcessId} offline", process.Id);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("ProcessCleanup completed. Processed {Count} stale processes.", staleProcesses.Length);

        return actions.ToArray();
    }*/
}

