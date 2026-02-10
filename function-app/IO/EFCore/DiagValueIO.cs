using DiagnosticExplorer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.EFCore;

internal class DiagValueIO : IDiagValueIO
{
    private readonly DiagDbContext _context;
    private readonly ILogger _logger;

    public DiagValueIO(DiagDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Save(DiagValues values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));

        try
        {
            var existing = await _context.DiagValues
                .WithPartitionKey(values.SiteId)
                .FirstOrDefaultAsync(v => v.Id == values.Id);

            if (existing == null)
            {
                _context.DiagValues.Add(values);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(values);
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new ApplicationException($"EF Core error while saving DiagValue with Id: {values.Id}", ex);
        }
    }

    public async Task<DiagValues?> Get(string processId, string siteId)
    {
        if (string.IsNullOrEmpty(processId)) throw new ArgumentNullException(nameof(processId));
        if (string.IsNullOrEmpty(siteId)) throw new ArgumentNullException(nameof(siteId));

        try
        {
            return await _context.DiagValues
                .WithPartitionKey(siteId)
                .FirstOrDefaultAsync(v => v.Id == processId);
        }
        catch (DbUpdateException ex)
        {
            throw new ApplicationException($"EF Core error while retrieving DiagValue with ProcessId: {processId} and SiteId: {siteId}", ex);
        }
    }

    public async Task DeleteForProcess(string processId, string siteId)
    {
        var value = await _context.DiagValues
            .WithPartitionKey(siteId)
            .FirstOrDefaultAsync(v => v.Id == processId);

        if (value != null)
        {
            _context.DiagValues.Remove(value);
            await _context.SaveChangesAsync();
        }
    }
}

