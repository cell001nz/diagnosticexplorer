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

    public WebHubTrigger(ILogger<WebHubTrigger> logger, IDiagIO diagIO) : base(logger, diagIO)
    {
    }

    #region Negotiate => GET /api/webclient/negotiate

    [Function("WebHub_negotiate")]
    public IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "webhub/negotiate")]
        HttpRequest req,
        [SignalRConnectionInfoInput(HubName = WEB_HUB, UserId = "{headers.x-ms-client-principal}")]
        SignalRConnectionInfo connectionInfo)
    {
        _logger.LogWarning($"### HEADERS ### {string.Join(", ", req.Headers.Keys)}");
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

        var cp = GetClientPrincipal(invocationContext.UserId);
        var account = await DiagIO.Account.GetAccount(cp.UserId)
            ?? throw new ApplicationException($"Account not found for user {invocationContext.UserId}");

        WebClient client = new()
        {
            Id = invocationContext.ConnectionId,
            AccountId = account.Id
        };
        await DiagIO.WebClient.Save(client);
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

        WebClient? client = await DiagIO.WebClient.Get(invocationContext.ConnectionId);

        if (client != null)
        {
            foreach (WebProcSub sub in client.Subscriptions.Values)
            { 
                await DiagIO.Process.DeleteWebSub(sub)
                    .Catch(ex => logger.LogError(ex, $"WebHub_OnClientDisconnected failed to delete process WebSub"));
            }
        }

        await DiagIO.WebClient.Delete(invocationContext.ConnectionId)
            .Catch(ex => logger.LogError(ex, "WebHub_OnClientDisconnected failed to delete WebClient"));
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

        // var processes = await DiagIO.Process.GetProcessesForSite(siteId);

        return
        [
            new SignalRGroupAction(SignalRGroupActionType.Add)
            {
                GroupName = siteId,
                ConnectionId = invokeContext.ConnectionId
            },
            // new SignalRMessageAction("receiveProcesses", [siteId, processes])
        ];
    }

    #endregion

    #region UnsubscribeSite => SIGNALR/UnsubscribeSite

    [Function("ClientHub_UnsubscribeSite")]
    [SignalROutput(HubName = WEB_HUB)]
    public SignalRGroupAction UnsubscribeSite(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(UnsubscribeSite), nameof(siteId))]
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

    #region SubscribeProcess => SIGNALR/SubscribeProcess

    [Function("ClientHub_SubscribeProcess")]
    public async Task<DualHubOutput> SubscribeProcess(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(SubscribeProcess), nameof(processId), nameof(siteId))]
        SignalRInvocationContext invokeContext,
        string processId, string siteId,
        FunctionContext context)
    {
        var logger = context.GetLogger("ClientHub_SubscribeProcess");
        logger.LogWarning($"Connection {invokeContext.ConnectionId} SUBSCRIBE to {processId}");

        DiagProcess process = await DiagIO.Process.GetProcess(processId, siteId)
                              ?? throw new ApplicationException($"Can't find Process {processId}/{siteId}");

        WebProcSub sub = new()
        {
            ProcessId = process.Id,
            SiteId = process.SiteId,
            WebConnectionId = invokeContext.ConnectionId
        };
        await DiagIO.Process.SaveWebSub(sub);
        await DiagIO.WebClient.SaveWebSub(sub);

        DualHubOutput output = new();

        //If the process is not sending already, instruct it to start sending diagnostics
        if (!process.IsSending || process.LastReceived - DateTime.UtcNow > TimeSpan.FromSeconds(DIAG_SEND_FREQ * 2))
        {
            await DiagIO.Process.SetProcessSending(process.Id, process.SiteId, true);
            output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.StartSending, [DIAG_SEND_FREQ]) 
                { ConnectionId = process.ConnectionId });
        }

        output.WebClient.Add(new SignalRGroupAction(SignalRGroupActionType.Add)
        {
            GroupName = processId,
            ConnectionId = invokeContext.ConnectionId
        });
        
        return output;
    }

    #endregion

    #region UnsubscribeProcess => SIGNALR/UnsubscribeProcess

    [Function("ClientHub_UnsubscribeProcess")]
    public async Task<DualHubOutput> UnsubscribeProcess(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(UnsubscribeProcess), nameof(processId), nameof(siteId))]
        SignalRInvocationContext invokeContext,
        string processId,
        string siteId,
        FunctionContext context)
    {
        var logger = context.GetLogger("ClientHub_SubscribeProcess");
        logger.LogWarning($"Connection {invokeContext.ConnectionId} UNSUBSCRIBE to {processId}");

        DualHubOutput output = new();
        
        DiagProcess process = await DiagIO.Process.GetProcess(processId, siteId)
                              ?? throw new ApplicationException($"Can't find Process {processId}/{siteId}");

        WebProcSub sub = new()
        {
            ProcessId = processId,
            SiteId = siteId,
            WebConnectionId = invokeContext.ConnectionId
        };

        await DiagIO.Process.DeleteWebSub(sub).Catch(ex => logger.LogError(ex, $"DiagIO.Process.DeleteWebSub failed"));
        await DiagIO.WebClient.DeleteWebSub(sub).Catch(ex => logger.LogError(ex, $"DiagIO.WebClient.DeleteWebSub failed"));

        output.WebClient.Add(new SignalRGroupAction(SignalRGroupActionType.Remove) 
            { ConnectionId = invokeContext.ConnectionId});

        process.Subscriptions.Remove(invokeContext.ConnectionId);

        if (process.Subscriptions.Count == 0)
        {
            output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.StopSending)
                { ConnectionId = process.ConnectionId });
        }

        return output;
    }

    #endregion

    #region SetProperty => SIGNALR/SetProperty

    [Function("ProcessHub_SetProperty")]
    [SignalROutput(HubName = PROCESS_HUB)]
    public async Task<SignalRMessageAction> SetProperty(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(SetProperty), nameof(request))]
        SignalRInvocationContext invokeContext,
        SetPropertyRequest request,
        FunctionContext context)
    {
        var process = await DiagIO.Process.GetProcess(request.ProcessId, request.SiteId)
            ?? throw new ApplicationException($"Process {request.ProcessId} not found for site {request.SiteId}");

        if (string.IsNullOrWhiteSpace(process.ConnectionId))
            throw new ApplicationException($"Process {request.ProcessId} is not connected");

        return new SignalRMessageAction("SetProperty", ["asdf", request.Path, request.Value ?? ""])
        {
            ConnectionId = process.ConnectionId
        };
    }

    #endregion
   
}