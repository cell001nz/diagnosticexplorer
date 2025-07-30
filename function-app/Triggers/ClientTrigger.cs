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

public class ClientTrigger : TriggerBase
{
    private const int RENEW_TIME = 20;

    public ClientTrigger(ILogger<AccountTrigger> logger, IDiagIO diagIo) : base(logger, diagIo)
    {
    }

    #region Negotiate => GET /api/client/negotiate

    [Function("Client_negotiate")]
    public IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client/negotiate")]
        HttpRequest req,
        [SignalRConnectionInfoInput(HubName = CLIENT_HUB)]
        SignalRConnectionInfo connectionInfo)
    {
        Console.WriteLine($"ConnectionInfo is null: {connectionInfo == null} {connectionInfo?.Url} {connectionInfo?.AccessToken}");
        return new OkObjectResult(connectionInfo);
    }
    
    #endregion
    
    #region OnSignalRDisconnected => EVENTGRID

    [Function("Client_OnSignalRDisconnected")]
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

    [Function("Client_Register")]
    [SignalROutput(HubName = CLIENT_HUB)]
    public async Task<SignalRMessageAction?> Register(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(Register), nameof(registration))]
        SignalRInvocationContext invokeContext,
        Registration registration,
        FunctionContext context)
    {
        DiagProcess[] processes = await DiagIO.Process.GetProcessesForSite(registration.Code);

        var process = processes.FirstOrDefault(p => p.ConnectionId == invokeContext.ConnectionId && p.InstanceId == registration.InstanceId);

        //If we've found exactly the process we want, just make sure it's online and update the lastOnline timne
        if (process != null)
        {
            await DiagIO.Process.SetOnline(process.Id, registration.Code);
        }
        else
        {
            Site site = await DiagIO.Site.GetSite(registration.Code);
            VerifyClientSecret(site, registration);

            process = processes.FirstOrDefault(p => p.InstanceId == registration.InstanceId)
                      ?? processes.FirstOrDefault(p => !p.IsOnline)
                      ?? processes.FirstOrDefault(p => DateTime.UtcNow - p.LastOnline > TimeSpan.FromMinutes(5))
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
        }

        return registration.RenewTimeSeconds == RENEW_TIME
            ? null
            : new SignalRMessageAction("SetRenewTime", [RENEW_TIME])
            {
                ConnectionId = invokeContext.ConnectionId
            };
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

    [Function("Client_Deregister")]
    public async Task Deregister(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(Deregister))]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        DiagProcess? found = await DiagIO.Process.GetProcessForConnectionId(invokeContext.ConnectionId);
        
        if (found != null)
        {
            found.IsOnline = false;
            await DiagIO.Process.SetOffline(found.Id, found.SiteId);
        }
    }

    #endregion

    #region ReceiveDiagnostics => SIGNALR/ReceiveDiagnostics

    [Function("Client_ReceiveDiagnostics")]
    [SignalROutput(HubName = CLIENT_HUB)]
    public async Task<SignalRMessageAction?> ReceiveDiagnostics(
        [SignalRTrigger(CLIENT_HUB, MESSAGES, nameof(ReceiveDiagnostics), nameof(stringData))]
        SignalRInvocationContext invokeContext,
        string stringData,
        FunctionContext context)
    {
        
        byte[] data = Convert.FromBase64String(stringData);
        Console.WriteLine($"Received diagnostics from {invokeContext.ConnectionId} {data.Length} bytes hash {HashHelper.ComputeHashString(data)} data[0]: {data[0]}");
        Trace.WriteLine($"Received diagnostics from {invokeContext.ConnectionId} {data.Length} bytes hash {HashHelper.ComputeHashString(data)} data[0]: {data[0]}");
        
        DiagProcess process = await GetProcessOrThrow(invokeContext);
        
        if (!process.IsSending)
            await DiagIO.Process.SetOnline(process.Id, process.SiteId);

        // DiagnosticResponse response = ProtobufUtil.Decompress<DiagnosticResponse>(data);
        DiagValues values = new DiagValues()
        {
            Id = process.Id,
            SiteId = process.SiteId,
            Date = DateTime.UtcNow,
            Response = stringData
        };
        await DiagIO.Values.Save(values);


        return new SignalRMessageAction("ReceiveMessage", [$"Got your diagnostics data.Length??? bytes"])
        {
            ConnectionId = invokeContext.ConnectionId
        };
    }

    #endregion

    #region ClearEvents => SIGNALR/ClearEventStream

    [Function("Client_ClearEventStream")]
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

    [Function("Client_StreamEvents")]
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

    private async Task<DiagProcess> GetProcessOrThrow(SignalRInvocationContext invokeContext)
    {
        DiagProcess? process = await DiagIO.Process.GetProcessForConnectionId(invokeContext.ConnectionId);
        if (process == null)
            throw new ApplicationException($"Can't find process for connectionId {invokeContext.ConnectionId}");

        return process;
    }

}