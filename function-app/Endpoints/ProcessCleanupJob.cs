using DiagnosticExplorer.DataAccess;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.Endpoints;

public class ProcessCleanupJob : ApiBase
{
    public ProcessCleanupJob(ILogger<ProcessCleanupJob> logger, DiagDbContext context) : base(logger, context)
    {
    }

    /// <summary>
    /// Timer trigger that runs every minute to mark stale online processes as offline.
    /// Processes are considered stale if they haven't been online for more than 2 minutes.
    /// </summary>
    [Function("ProcessCleanup")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo)
    {
        

        /*
        _logger.LogInformation("ProcessCleanup timer triggered at: {Time}", DateTime.UtcNow);

        var cutoffTime = DateTime.UtcNow.AddMinutes(-2);
        var staleProcesses = await DiagIO.Process.GetStaleOnlineProcesses(cutoffTime);

        _logger.LogInformation("Found {Count} stale online processes", staleProcesses.Length);

        foreach (var process in staleProcesses)
        {
            try
            {
                await DiagIO.Process.SetOffline(process.Id, process.SiteId);
                _logger.LogInformation("Set process {ProcessId} offline (last online: {LastOnline})", 
                    process.Id, process.LastOnline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set process {ProcessId} offline", process.Id);
            }
        }

        _logger.LogInformation("ProcessCleanup completed. Processed {Count} stale processes.", staleProcesses.Length);*/
    }
}

