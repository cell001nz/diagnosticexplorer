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
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace api.Triggers;

public class ProcessHubTrigger : TriggerBase
{
    private const int RENEW_TIME = 20;
    private const int STALE_TIME = 60;

    public ProcessHubTrigger(ILogger<ProcessHubTrigger> logger, IDiagIO diagIO) : base(logger, diagIO)
    {
    }

    #region Negotiate => GET /api/processhub/negotiate

    [Function("ProcessHub_negotiate")]
    public IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "processhub/negotiate")]
        HttpRequest req,
        [SignalRConnectionInfoInput(HubName = CLIENT_HUB)]
        SignalRConnectionInfo connectionInfo)
    {
        Console.WriteLine($"ConnectionInfo is null: {connectionInfo?.Url} {connectionInfo?.AccessToken}");
        return new OkObjectResult(connectionInfo);
    }
    
    #endregion
    
    #region OnSignalRDisconnected => EVENTGRID

    [Function("ProcessHub_OnSignalRDisconnected")]
    public void OnSignalRDisconnected(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        FunctionContext context)
    {
        var logger = context.GetLogger("OnSignalRDisconnected");
        logger.LogInformation($"Event type: {eventGridEvent.EventType}");
        logger.LogInformation($"Data: {eventGridEvent.Data}");
        Console.WriteLine("######################################## OnSignalRDisconnected");
        // Optional: parse eventGridEvent.Data for details (e.g., connectionId, userId)
    }
    #endregion

    #region Register => SIGNALR/Register

    [Function("ProcessHub_Register")]
    public async Task<DualHubOutput> Register(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(Register), nameof(registration))]
        SignalRInvocationContext invokeContext,
        Registration registration,
        FunctionContext context)
    {
        DualHubOutput output = new DualHubOutput();
        var logger = context.GetLogger(typeof(ProcessHubTrigger).FullName);
        try
        {
            DiagProcess[] processes = await DiagIO.Process
                .GetCandidateProcesses(registration.Code, registration.ProcessName, registration.MachineName, registration.UserName);

            var process = processes.FirstOrDefault(p => p.ConnectionId == invokeContext.ConnectionId && p.InstanceId == registration.InstanceId);
            bool wasOnline = process?.IsOnline ?? false;

            //If we've found exactly the process we want, just make sure it's online and update the lastOnline time
            if (process != null)
            {
                await DiagIO.Process.SetOnline(process.Id, registration.Code, DateTime.UtcNow);
            }
            else
            {
                Site site = await DiagIO.Site.GetSite(registration.Code);
                VerifyClientSecret(site, registration);

                process = processes.FirstOrDefault(p => p.InstanceId == registration.InstanceId)
                          ?? processes.FirstOrDefault(p => !p.IsOnline)
                          ?? processes.FirstOrDefault(p => DateTime.UtcNow - p.LastOnline > TimeSpan.FromSeconds(STALE_TIME))
                          ?? new DiagProcess
                          {
                              Id = Guid.NewGuid().ToString("N"),
                              SiteId = registration.Code,
                              ProcessName = registration.ProcessName,
                              MachineName = registration.MachineName,
                              UserName = registration.UserName
                          };

                process.ConnectionId = invokeContext.ConnectionId;
                process.InstanceId = registration.InstanceId;
                process.LastOnline = DateTime.UtcNow;
                process.IsOnline = true;

                await DiagIO.Process.SaveProcess(process);

                //If it's a new connection or has come through on a different connectionId, we may have to tell it to start sending
                if (process.IsSending)
                {
                    output.ProcessClient.Add(new SignalRMessageAction("StartSending", [2]) { ConnectionId = invokeContext.ConnectionId });
                }
            }

            if (!wasOnline)
            {
                output.WebClient.Add(new SignalRMessageAction("ReceiveProcess", [process]) { GroupName = process.SiteId });
            }

            if (registration.RenewTimeSeconds != RENEW_TIME)
            {
                output.ProcessClient.Add(new SignalRMessageAction("SetRenewTime", [RENEW_TIME])
                {
                    ConnectionId = invokeContext.ConnectionId
                });
            }
            
            DiagProcess[] remaining = processes
                .Where(p => p != process)
                .Where(p => !p.IsOnline || DateTime.UtcNow - p.LastOnline > TimeSpan.FromSeconds(STALE_TIME))
                .ToArray();

            DeleteProcesses(remaining, logger);

            return output;
        }
        catch (Exception ex)
        {
            logger.LogError($"Register failed {ex}");
            throw;
        }
    }
    
    private void DeleteProcesses(DiagProcess[] processes, ILogger logger)
    {
        foreach (var process in processes)
        {
            try
            {
                DiagIO.SinkEvent.DeleteForProcess(process.Id);
                DiagIO.Values.DeleteForProcess(process.Id, process.SiteId);
                DiagIO.Process.Delete(process.Id, process.SiteId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to delete processes");
            }
        }
    }

    private void VerifyClientSecret(Site site, Registration registration)
    {
        if (site is not { Secrets.Count: > 0 })
            throw new ApplicationException($"Site {registration.Code} has no secrets defined");

        PasswordHasher hasher = new PasswordHasher();
        string secretHash = hasher.HashSecret(registration.Secret);
        bool secretOk = site.Secrets.Any(secret => secret.Hash == secretHash);
        if (!secretOk)
            throw new ApplicationException("Invalid secret");
    }

    #endregion

    #region Deregister => SIGNALR/Deregister

    [Function("ProcessHub_Deregister")]
    public async Task<SignalRMessageAction?> Deregister(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(Deregister))]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        var logger = context.GetLogger(typeof(ProcessHubTrigger).FullName!);

        DiagProcess? process = await DiagIO.Process.GetProcessForConnectionId(invokeContext.ConnectionId);
        
        if (process != null)
        {
            process.IsOnline = false;
            await DiagIO.Process.SetOffline(process.Id, process.SiteId);
            logger.LogWarning($"Deregister sending ReceiveProcess to GroupName {process.SiteId}");
            return new SignalRMessageAction("ReceiveProcess", [process]) { GroupName = process.SiteId };
        }

        return null;
    }

    #endregion

    #region ReceiveDiagnostics => SIGNALR/ReceiveDiagnostics

    [Function("ProcessHub_ReceiveDiagnostics")]
    [SignalROutput(HubName = CLIENT_HUB)]
    public async Task<SignalRMessageAction?> ReceiveDiagnostics(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(ReceiveDiagnostics), nameof(stringData))]
        SignalRInvocationContext invokeContext,
        string stringData,
        FunctionContext context)
    {
        DiagProcess process = await GetProcessOrThrow(invokeContext);
        
        DiagValues values = new DiagValues()
        {
            Id = process.Id,
            SiteId = process.SiteId,
            Date = DateTime.UtcNow,
            Response = stringData
        };
        await DiagIO.Values.Save(values);
        await DiagIO.Process.SetLastReceived(process.Id, process.SiteId, DateTime.UtcNow);

        if (!process.IsSending)
        {
            return new SignalRMessageAction("StopSending")
            {
                ConnectionId = invokeContext.ConnectionId
            };
        }
        
        return new SignalRMessageAction("ReceiveMessage", [$"Got your diagnostics data.Length??? bytes"])
        {
            ConnectionId = invokeContext.ConnectionId
        };
    }

    #endregion

    #region ClearEvents => SIGNALR/ClearEventStream

    [Function("ProcessHub_ClearEventStream")]
    public async Task<IActionResult> ClearEventStream(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(ClearEventStream))]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        DiagProcess process = await GetProcessOrThrow(invokeContext);

        await DiagIO.SinkEvent.DeleteForProcess(process.Id);
        
        var result = new RpcResult
        {
            IsSuccess = true
        };
        return new OkObjectResult(result);
    }
    #endregion

    #region StreamEvents => POST /api/client/StreamEvents

    [Function("ProcessHub_StreamEvents")]
    public async Task StreamEvents(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(StreamEvents), nameof(events))]
        SignalRInvocationContext invokeContext,
        SystemEvent[] events,
        FunctionContext context)
    {
        
        DiagProcess process = await GetProcessOrThrow(invokeContext);
        // byte[] data = Convert.FromBase64String(eventData);
        // SystemEvent[] events = ProtobufUtil.Decompress<SystemEvent[]>(data);
        foreach (var evt in events)
            evt.ProcessId = process.Id;

        await DiagIO.SinkEvent.Save(events);
    }

    #endregion

    #region OnConnected => SIGNALR/connections/connected

    [Function("OnClientConnected")]
    public async Task OnConnected(
        [SignalRTrigger(CLIENT_HUB, "connections", "connected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OnConnected));
        
        logger.LogInformation($"----------------------------------- ClientTrigger.OnConnected {invocationContext.ConnectionId}");
    }

    #endregion

    #region OnDisconnected => SIGNALR/connections/disconnected

    [Function("ProcessHub_OnProcessDisconnected")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<SignalRMessageAction?> OnDisconnected(
        [SignalRTrigger(CLIENT_HUB, "connections", "disconnected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OnDisconnected));
        logger.LogInformation($"----------------------------------- ClientTrigger.OnDisconnected {invocationContext.ConnectionId}");

        var process = await DiagIO.Process.GetProcessForConnectionId(invocationContext.ConnectionId);
        if (process != null)
        {
            process.IsOnline = false;
            process.ConnectionId = null;
            await DiagIO.Process.SetOffline(process.Id, process.SiteId);

            logger.LogWarning($"OnDisconnected sending ReceiveProcess to GroupName {process.SiteId}");
            return new SignalRMessageAction("ReceiveProcess", [process]) { GroupName = process.SiteId };
        }

        return null;
    }
    
    #endregion
    
    private async Task<DiagProcess> GetProcessOrThrow(SignalRInvocationContext invokeContext)
    {
        DiagProcess? process = await DiagIO.Process.GetProcessForConnectionId(invokeContext.ConnectionId);
        if (process == null)
            throw new ApplicationException($"Can't find process for connectionId {invokeContext.ConnectionId}");

        return process;
    }

    public class DualHubOutput
    {
        [SignalROutput(HubName = WEB_HUB)]
        public List<object> WebClient { get; } = [];
        
        [SignalROutput(HubName = CLIENT_HUB)]
        public List<object> ProcessClient { get; } = [];
    }

}