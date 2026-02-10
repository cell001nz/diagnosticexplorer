using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.EFCore;

internal class SinkEventIO : ISinkEventIO
{
    private readonly DiagDbContext _context;
    private readonly ILogger _logger;

    public SinkEventIO(DiagDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task DeleteForProcess(string processId)
    {
        var events = await _context.SinkEvents
            .WithPartitionKey(processId)
            .Where(e => e.ProcessId == processId)
            .ToListAsync();

        _context.SinkEvents.RemoveRange(events);
        await _context.SaveChangesAsync();
    }

    public async Task Save(SystemEvent[] events)
    {
        foreach (var evt in events)
        {
            evt.Id = Guid.NewGuid().ToString("N");
        }

        _context.SinkEvents.AddRange(events);
        await _context.SaveChangesAsync();
    }
}

