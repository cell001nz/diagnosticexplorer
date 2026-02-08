using Azure.Messaging.EventGrid;
using DiagnosticExplorer;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Api.Security;
using DiagnosticExplorer.Api.Triggers;
using DiagnosticExplorer.Domain;
using DiagnosticExplorer.IO;
using DiagnosticExplorer.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.Connections;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace api.Triggers;

public class ProcessHubTrigger : TriggerBase
{
    private const string PROCESS_CLAIM = "DiagProcessClaim";
    
    private readonly ServiceManager _serviceManager;
    
    public ProcessHubTrigger(ILogger<ProcessHubTrigger> logger, IDiagIO diagIO) : base(logger, diagIO)
    {
        _serviceManager = new ServiceManagerBuilder()
            .WithOptions(o =>
            {
                o.ConnectionString = Environment.GetEnvironmentVariable("AzureSignalRConnectionString");
            }).BuildServiceManager();
    }

    #region Negotiate => POST /api/processhub/negotiate

    [Function("ProcessHub_negotiate")]
    public async Task<IActionResult> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "processhub/negotiate")]
        HttpRequest req
        // [SignalRConnectionInfoInput(HubName = PROCESS_HUB)]
        // SignalRConnectionInfo connectionInfo
        )
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Registration? registration = JsonSerializer.Deserialize<Registration>(requestBody, DiagJsonOptions.Default);
        
        if (registration?.Code == null || registration.Secret == null)
            return new BadRequestObjectResult("Missing clientId or secret");

        Site site = await DiagIO.Site.GetSite(registration.Code);
        if (!VerifyClientSecret(site, registration))
            return new UnauthorizedObjectResult("Invalid client code or secret");
        
        DiagProcess process = await RegisterProcess(registration);
        List<Claim> customClaims =
        [
            new Claim(PROCESS_CLAIM, $"{process.Id}/{process.SiteId}")
        ];

        var hubContext = await _serviceManager.CreateHubContextAsync(PROCESS_HUB, CancellationToken.None);
        NegotiationResponse? thing = await hubContext.NegotiateAsync(new NegotiationOptions()
        {
            UserId = $"{process.Id}/{process.SiteId}",
            Claims = customClaims
        });
        
        if (thing == null)
            return new BadRequestObjectResult("Failed to negotiate SignalR connection");

        SignalRConnectionInfo info = new SignalRConnectionInfo()
        {
            Url = thing.Url,
            AccessToken = thing.AccessToken
        };
        
        // Console.WriteLine($"ConnectionInfo is null: {connectionInfo?.Url} {connectionInfo?.AccessToken}");
        return new OkObjectResult(info);
    }
    
    #endregion
    
    #region OnConnected => SIGNALR/connections/connected

    [Function("OnClientConnected")]
    public async Task<DualHubOutput> OnConnected(
        [SignalRTrigger(PROCESS_HUB, "connections", "connected")]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        var logger = context.GetLogger(typeof(ProcessHubTrigger).FullName!);
        
        var (processId, siteId) = GetProcessAndSiteId(invokeContext);
        logger.LogInformation($"----------------------------------- ProcessHubTrigger.OnConnected {invokeContext.ConnectionId}/{processId}/{siteId}");

        // var logger = context.GetLogger(nameof(OnConnected));
        await DiagIO.Process.SetConnectionId(processId, siteId, invokeContext.ConnectionId);

        DiagProcess process = await GetProcess(invokeContext);

        DualHubOutput output = new DualHubOutput();
        output.WebClient.Add(new SignalRMessageAction(Messages.Web.ReceiveProcess, [process]) { GroupName = process.SiteId });

        output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.SetRenewTime, [PROCESS_RENEW_TIME])
        {
            ConnectionId = invokeContext.ConnectionId
        });

        if (process.Subscriptions.Any())
        {
            output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.StartSending, [DIAG_SEND_FREQ])
                { ConnectionId = invokeContext.ConnectionId });
        }

        return output;
    }

    #endregion

    #region OnDisconnected => SIGNALR/connections/disconnected

    [Function("ProcessHub_OnProcessDisconnected")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<SignalRMessageAction?> OnDisconnected(
        [SignalRTrigger(PROCESS_HUB, "connections", "disconnected")] SignalRInvocationContext invokeContext, 
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OnDisconnected));
        logger.LogInformation($"----------------------------------- ClientTrigger.OnDisconnected {invokeContext.ConnectionId}");
        var process = await GetProcess(invokeContext);
        // var process = await DiagIO.Process.GetProcessForConnectionId(invokeContext.ConnectionId);
        // if (process != null)
        // {
            await DiagIO.Process.SetOffline(process.Id, process.SiteId);

            process.IsOnline = false;
            process.IsSending = false;
            process.ConnectionId = null;
            process.InstanceId = null;
            
            logger.LogWarning($"OnDisconnected sending ReceiveProcess to GroupName {process.SiteId}");
            return new SignalRMessageAction("ReceiveProcess", [process]) { GroupName = process.SiteId };
        // }

        return null;
    }
    
    #endregion
    
    
    #region Register => SIGNALR/Register

    /*[Function("ProcessHub_Register")]
    public async Task<DualHubOutput> Register(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(Register), nameof(registration))]
        SignalRInvocationContext invokeContext,
        Registration registration,
        FunctionContext context)
    {
        DualHubOutput output = new DualHubOutput();
        var logger = context.GetLogger(typeof(ProcessHubTrigger).FullName);
        try
        {
            
            DiagProcess process = await RegisterProcess(registration);
            if (process.IsSending)
            {
                output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.StartSending, [DIAG_SEND_FREQ]) 
                    { ConnectionId = invokeContext.ConnectionId });
            }

            DiagProcess[] processes = await DiagIO.Process
                .GetCandidateProcesses(registration.Code, registration.ProcessName, registration.MachineName, registration.UserName);

            var process = processes.FirstOrDefault(p => p.ConnectionId == invokeContext.ConnectionId && p.InstanceId == registration.InstanceId);
            bool wasOnline = process?.IsOnline ?? false;

            //If we've found exactly the process we want, just make sure it's online and update the lastOnline time
            if (process != null)
            {
                await DiagIO.Process.SetOnline(process.Id, process.SiteId, DateTime.UtcNow);
            }
            else
            {
                Site site = await DiagIO.Site.GetSite(registration.Code);
                VerifyClientSecret(site, registration);

                process = processes.FirstOrDefault(p => p.InstanceId == registration.InstanceId)
                          ?? processes.FirstOrDefault(p => !p.IsOnline)
                          ?? processes.FirstOrDefault(p => DateTime.UtcNow - p.LastOnline > TimeSpan.FromSeconds(PROCESS_STALE_TIME))
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
                    output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.StartSending, [DIAG_SEND_FREQ]) 
                        { ConnectionId = invokeContext.ConnectionId });
                }
            }

            if (!wasOnline)
            {
                output.WebClient.Add(new SignalRMessageAction(Messages.Web.ReceiveProcess, [process]) { GroupName = process.SiteId });
            }

            if (registration.RenewTimeSeconds != PROCESS_RENEW_TIME)
            {
                output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.SetRenewTime, [PROCESS_RENEW_TIME])
                {
                    ConnectionId = invokeContext.ConnectionId
                });
            }
            
            DiagProcess[] remaining = processes
                .Where(p => p != process)
                .Where(p => !p.IsOnline || DateTime.UtcNow - p.LastOnline > TimeSpan.FromSeconds(PROCESS_STALE_TIME))
                .ToArray();

            DeleteProcesses(remaining, logger);

            return output;
        }
        catch (Exception ex)
        {
            logger.LogError($"Register failed {ex}");
            throw;
        }
    }*/

   
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

    private bool VerifyClientSecret(Site site, Registration registration)
    {
        if (site is not { Secrets.Count: > 0 })
            return false;

        PasswordHasher hasher = new PasswordHasher();
        string secretHash = hasher.HashSecret(registration.Secret);
        return site.Secrets.Any(secret => secret.Hash == secretHash);
    }

    #endregion

    #region ReceiveDiagnostics => SIGNALR/ReceiveDiagnostics

    [Function("ProcessHub_ReceiveDiagnostics")]
    public async Task<DualHubOutput> ReceiveDiagnostics(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(ReceiveDiagnostics), nameof(stringData))]
        SignalRInvocationContext invokeContext,
        string stringData,
        FunctionContext context)
    {
        // DiagProcess process = await GetProcess(invokeContext);
        var logger = context.GetLogger(typeof(ProcessHubTrigger).FullName!);
        logger.LogWarning($"ReceiveDiagnostics", stringData);
        DualHubOutput output = new DualHubOutput();

        // DiagValues values = new DiagValues()
        // {
        // Id = process.Id,
        // SiteId = process.SiteId,
        // Date = DateTime.UtcNow,
        // Response = stringData
        // };
        // await DiagIO.Values.Save(values);
        // await DiagIO.Process.SetLastReceived(process.Id, process.SiteId, DateTime.UtcNow);

        var (processId, siteId) = GetProcessAndSiteId(invokeContext);

        // if (!process.IsSending)
        // {
            // output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.StopSending)
            // {
                // ConnectionId = invokeContext.ConnectionId
            // });
        // }
        // else
        // {
            DiagnosticResponse response = DeserialiseBase64Protobuf<DiagnosticResponse>(stringData);
            response.ServerDate = DateTime.UtcNow;

            output.WebClient.Add(new SignalRMessageAction(Messages.Web.ReceiveDiagnostics, [processId, response])
            {
                GroupName = processId
            });
        // }

        return output;
    }

    #endregion

    #region ClearEvents => SIGNALR/ClearEventStream

    /*[Function("ProcessHub_ClearEventStream")]
    public async Task<IActionResult> ClearEventStream(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(ClearEventStream))]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        DiagProcess process = await GetProcess(invokeContext);

        await DiagIO.SinkEvent.DeleteForProcess(process.Id);
        
        var result = new RpcResult
        {
            IsSuccess = true
        };
        return new OkObjectResult(result);
    }*/
    
    #endregion

    #region StreamEvents => SIGNALR/StreamEvents

    [Function("ProcessHub_StreamEvents")]
    public async Task StreamEvents(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(StreamEvents), nameof(events))]
        SignalRInvocationContext invokeContext,
        SystemEvent[] events,
        FunctionContext context)
    {
        
        DiagProcess process = await GetProcess(invokeContext);
        // byte[] data = Convert.FromBase64String(eventData);
        // SystemEvent[] events = ProtobufUtil.Decompress<SystemEvent[]>(data);
        foreach (var evt in events)
            evt.ProcessId = process.Id;

        await DiagIO.SinkEvent.Save(events);
    }

    #endregion

    #region RegisterProcess

    private async Task<DiagProcess> RegisterProcess(Registration registration)
    {
        DiagProcess[] processes = await DiagIO.Process
            .GetCandidateProcesses(registration.Code, registration.ProcessName, registration.MachineName, registration.UserName);

        var process = processes.FirstOrDefault(p => p.InstanceId == registration.InstanceId)
                      ?? processes.FirstOrDefault(p => !p.IsOnline)
                      ?? processes.FirstOrDefault(p => DateTime.UtcNow - p.LastOnline > TimeSpan.FromSeconds(PROCESS_STALE_TIME))
                      ?? new DiagProcess
                      {
                          Id = Guid.NewGuid().ToString("N"),
                          SiteId = registration.Code,
                          ProcessName = registration.ProcessName,
                          MachineName = registration.MachineName,
                          UserName = registration.UserName
                      };

        process.InstanceId = registration.InstanceId;
        process.LastOnline = DateTime.UtcNow;
        process.IsOnline = true;

        await DiagIO.Process.SaveProcess(process);

        return process;
    }

    #endregion
  
    (string processId, string siteId) GetProcessAndSiteId(SignalRInvocationContext invokeContext)
    {
        // if (!invokeContext.Claims.TryGetValue(PROCESS_CLAIM, out var vals) || vals[0] == null)
            // throw new ApplicationException($"Can't find diagnostics process claim");
        
        
        if (invokeContext.UserId == null)
            throw new ApplicationException("UserId is null in SignalRInvocationContext");

        // string value = vals[0]!;
        string value = invokeContext.UserId;
        
        string[] parts = value.Split('/');
        
        if (parts.Length != 2)
            throw new ApplicationException($"Invalid UserId format: {invokeContext.UserId}");
     
        return (parts[0], parts[1]);
    }

    private async Task<DiagProcess> GetProcess(SignalRInvocationContext invocationContext)
    {
        var (processId, siteId) = GetProcessAndSiteId(invocationContext);
        return await DiagIO.Process.GetProcess(processId, siteId)
               ?? throw new ApplicationException($"Process with ID {processId} not found for site {siteId}");
    }


}