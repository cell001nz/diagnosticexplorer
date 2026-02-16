using System.Linq.Expressions;
using DiagnosticExplorer.Api.Domain;
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

public class ProcessApi : ApiBase
{
    
    public ProcessApi(ILogger<ProcessApi> logger, DiagDbContext context) : base(logger, context)
    {
    }

    #region GetProcesses => GET /api/Sites/{siteId}/Processes

    [Function("GetProcesses")]
    public async Task<IActionResult> GetProcesses(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{siteId}/Processes")] HttpRequest req, 
        int siteId)
    {
        var account = await GetCurrentAccount(req);
        await VerifySiteAccess(account, siteId);

        DiagProcess[] processes = await _context.Processes.AsQueryable()
            .Where(p => p.SiteId == siteId)
            .Select(ProcessEntityUtil.Projection)
            .ToArrayAsync();
        
        return new OkObjectResult(processes);
    }

    #endregion

   
}