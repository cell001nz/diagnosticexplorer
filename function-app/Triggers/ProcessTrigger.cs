using Azure;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using DiagnosticExplorer.IO;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DiagnosticExplorer.Api.Triggers;

public class ProcessTrigger : TriggerBase
{
    public ProcessTrigger(ILogger<AccountTrigger> logger, IDiagIO diagIo) : base(logger, diagIo)
    {
    }

    #region GetProcesses => GET /api/Sites/{siteId}/Processes

    [Function("GetProcesses")]
    public async Task<IActionResult> GetProcesses(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{siteId}/Processes")] HttpRequest req, 
        string siteId, 
        ILogger log)
    {
        var cp = GetClientPrincipal(req);
        await VerifySiteAccess(cp, siteId);
        
        DiagProcess[] processes = await DiagIO.Process.GetProcessesForSite(siteId);
        return new OkObjectResult(processes);
    }

    #endregion

    #region GetProcesses => GET /api/Sites/{siteId}/Processes/{processId}/Diagnostics"

    [Function("GetDiagnostics")]
    [SignalROutput(HubName = CLIENT_HUB)]
    public async Task<SignalRMessageAction[]> GetDiagnostics(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{siteId}/Processes/{processId}/Diagnostics")] 
        HttpRequest req, 
        string siteId,
        string processId, ILogger log)
    {
        var cp = GetClientPrincipal(req);
        DiagProcess process = (await DiagIO.Process.GetProcess(processId, siteId))
                          ?? throw new ApplicationException($"Can't find process {processId}");
        
        await VerifySiteAccess(cp, siteId);
        
        req.HttpContext.Response.Headers.Add("Content-Type", "application/json");
        await req.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(process));

        return
        [
            new SignalRMessageAction("StartSending", [2])
            {
                ConnectionId = process.ConnectionId
            },
            new SignalRMessageAction("ReceiveMessage", ["Start Sending Diagnostics"])
            {
                ConnectionId = process.ConnectionId
            }
        ];
    }

    #endregion

  
}