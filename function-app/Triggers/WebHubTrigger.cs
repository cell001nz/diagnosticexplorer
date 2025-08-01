using System.Diagnostics;
using Azure.Messaging.EventGrid;
using DiagnosticExplorer;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Api.Security;
using DiagnosticExplorer.Api.Triggers;
using DiagnosticExplorer.Domain;
using DiagnosticExplorer.IO;
using DiagnosticExplorer.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace api.Triggers;

public class WebHubTrigger : TriggerBase
{

    public WebHubTrigger(ILogger<WebHubTrigger> logger, IDiagIO diagIo) : base(logger, diagIo)
    {
    }

    #region Negotiate => GET /api/webclient/negotiate

    [Function("WebHub_negotiate")]
    public IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "webhub/negotiate")]
        HttpRequest req,
        [SignalRConnectionInfoInput(HubName = WEB_HUB)]
        SignalRConnectionInfo connectionInfo)
    {
        return new OkObjectResult(connectionInfo);
    }
    
    #endregion
    
    #region OnConnected => SIGNALR/connections/connected
    
    [Function("WebHub_OnClientConnected")]
    public async Task OnConnected(
        [SignalRTrigger(WEB_HUB, "connections", "connected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OnConnected));
        logger.LogInformation($"----------------------------------- WebClient.OnConnected {invocationContext.ConnectionId}");
        
        // DiagIO.Process
        
    }

    #endregion

    #region OnDisconnected => SIGNALR/connections/disconnected

    [Function("WebHub_OnClientDisconnected")]
    public async Task OnDisconnected(
        [SignalRTrigger(WEB_HUB, "connections", "disconnected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OnDisconnected));
        logger.LogInformation($"----------------------------------- WebClient.OnDisconnected {invocationContext.ConnectionId}");

        var process = await DiagIO.Process.GetProcessForConnectionId(invocationContext.ConnectionId);
        if (process != null)
            await DiagIO.Process.SetOffline(process.Id, process.SiteId);

    }
    
    #endregion
    
    #region SubscribeSite => SIGNALR/SubscribeSite

    [Function("ClientHub_SubscribeSite")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<object[]> SubscribeSite(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(SubscribeSite), nameof(siteId))]
        SignalRInvocationContext invokeContext,
        string siteId,
        FunctionContext context)
    {
        var logger = context.GetLogger("ClientHub_SubscribeSite");
        logger.LogWarning($"Connection {invokeContext.ConnectionId} SUBSCRIBE to {siteId}");

        var processes = await DiagIO.Process.GetProcessesForSite(siteId);

        return
        [
            new SignalRGroupAction(SignalRGroupActionType.Add)
            {
                GroupName = siteId,
                ConnectionId = invokeContext.ConnectionId
            },
            new SignalRMessageAction("say", ["hello from the server"]),
            // new SignalRMessageAction("receiveProcesses", [siteId, processes])
        ];
    }

    #endregion

    #region SubscribeSite => SIGNALR/UnubscribeSite

    [Function("ClientHub_UnubscribeSite")]
    [SignalROutput(HubName = WEB_HUB)]
    public SignalRGroupAction UnubscribeSite(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(UnubscribeSite), nameof(siteId))]
        SignalRInvocationContext invokeContext,
        string siteId,
        FunctionContext context)
    {

        var logger = context.GetLogger("ClientHub_SubscribeSite");
        logger.LogWarning($"Connection {invokeContext.ConnectionId} UNSUBSCRIBE to {siteId}");

        return new SignalRGroupAction(SignalRGroupActionType.Remove)
        {
            GroupName = siteId,
            ConnectionId = invokeContext.ConnectionId
        };
    }

    #endregion

   
}