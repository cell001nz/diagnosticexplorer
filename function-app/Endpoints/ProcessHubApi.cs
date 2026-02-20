using System.Linq.Expressions;
using System.Security.Claims;
using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.Api.Security;
using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.DataExtensions;
using DiagnosticExplorer.Domain;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.SignalR.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DiagnosticExplorer.Endpoints;

public class ProcessHubApi : ApiBase
{
    private const string SITE_ID_CLAIM = "Diag_SiteId";

    private readonly ServiceManager _serviceManager;

    public ProcessHubApi(ILogger<ProcessHubApi> logger, DiagDbContext context) : base(logger, context)
    {
        _serviceManager = new ServiceManagerBuilder()
            .WithOptions(o => { o.ConnectionString = Environment.GetEnvironmentVariable("AzureSignalRConnectionString"); }).BuildServiceManager();
    }



    #region Negotiate => POST /api/processhub/negotiate

    [Function("ProcessHub_negotiate")]
    public async Task<IActionResult> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "processhub/negotiate")]
        HttpRequest req,
        [FromBody] SiteCredentials credentials)
    {
        _logger.LogWarning($"----------------------------------- ProcessHubTrigger.Negotiate {credentials?.Code}");

        if (credentials?.Code == null || credentials.Secret == null)
        {
            _logger.LogWarning($"----------------------------------- Missing clientId or secret");
            return new BadRequestObjectResult("Missing clientId or secret");
        }

        PasswordHasher hasher = new PasswordHasher();
        var secretHash = hasher.HashSecret(credentials.Secret);

        var site = await _context.Sites.Where(s => s.Code == credentials.Code
                                                   && s.Secrets.Any(sec => sec.Hash == secretHash))
            .FirstOrDefaultAsync();

        if (site == null)
            return new BadRequestObjectResult("Site not found");

        ServiceHubContext? hubContext = await _serviceManager.CreateHubContextAsync(PROCESS_HUB, CancellationToken.None);
        NegotiationResponse negResponse = await hubContext.NegotiateAsync(new NegotiationOptions()
                                          {
                                              Claims = [new Claim(SITE_ID_CLAIM, site.Id.ToString())]
                                          })
                                          ?? throw new ApplicationException("Failed to negotiate SignalR connection");

        if (string.IsNullOrWhiteSpace(negResponse.Url))
            throw new ApplicationException("Failed to negotiate SignalR connection - Url is null or empty");

        if (string.IsNullOrWhiteSpace(negResponse.AccessToken))
            throw new ApplicationException("Failed to negotiate SignalR connection - AccessToken is null or empty");

        ProcessNegotiateResponse response = new ProcessNegotiateResponse()
        {
            Url = negResponse.Url,
            AccessToken = negResponse.AccessToken,
            SiteId = site.Id
        };

        return new OkObjectResult(response);
    }


    #endregion

    /*#region OnConnected => SIGNALR/connections/connected

    [Function("OnClientConnected")]
    public async Task<DualHubOutput> OnConnected(
        [SignalRTrigger(PROCESS_HUB, "connections", "connected")]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
       
    }

    #endregion*/

    #region Register => POST /api/processhub/register

    [Function("ProcessHub_Register")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "processhub/register")]
        HttpRequest req,
        [FromBody] ProcessRegisterRequest request)
    {
        _logger.LogWarning($"----------------------------------- ProcessHubTrigger.Register {request?.Registration?.ProcessName} conn={request?.ConnectionId}");

        if (request?.ConnectionId == null || request.Registration == null)
            return new BadRequestObjectResult("Missing ConnectionId or Registration");

        int siteId = request.SiteId;
        var process = await RegisterProcess(siteId, request.ConnectionId, request.Registration);

        ServiceHubContext hubContext = await _serviceManager.CreateHubContextAsync(PROCESS_HUB, CancellationToken.None);

        await hubContext.Clients.Client(request.ConnectionId)
            .SendCoreAsync(nameof(IProcessHubClient.SetRenewTime), [PROCESS_RENEW_TIME_MILLIS]);

        await hubContext.Clients.Client(request.ConnectionId)
            .SendCoreAsync(nameof(IProcessHubClient.SetProcessId), [process.Id]);

        int subs = await _context.Processes.Where(p => p.Id == process.Id)
            .Select(p => p.Subscriptions.Count)
            .FirstOrDefaultAsync();

        if (subs > 0)
        {
            await hubContext.Clients.Client(request.ConnectionId)
                .SendCoreAsync(nameof(IProcessHubClient.StartSending), [DIAG_SEND_FREQ_MILLIS]);
        }

        await hubContext.Groups.AddToGroupAsync(request.ConnectionId, process.Id.ToString());

        await hubContext.Clients.Group(siteId.ToString())
            .SendCoreAsync(nameof(IWebHubClient.ReceiveProcess), [process]);

        return new OkObjectResult(process);
    }

    private async Task<DiagProcess> RegisterProcess(int siteId, string connectionId, Registration registration)
    {
        Expression<Func<ProcessEntity, bool>> processFilter = p => p.InstanceId == registration.InstanceId
                                                                   || (p.MachineName == registration.MachineName && p.UserName == registration.UserName && p.Name == registration.ProcessName);

        var candidates = await _context.Processes
            .Where(p => p.SiteId == siteId)
            .Where(processFilter)
            .ToArrayAsync();

        var process = candidates.FirstOrDefault(p => p.InstanceId == registration.InstanceId)
                      ?? candidates.FirstOrDefault(p => !p.IsOnline)
                      ?? candidates.FirstOrDefault(p => DateTime.UtcNow - p.LastOnline > TimeSpan.FromMilliseconds(PROCESS_STALE_TIME_MILLIS))
                      ?? _context.Processes.Add(new ProcessEntity()
                      {
                          SiteId = siteId,
                          Name = registration.ProcessName,
                          MachineName = registration.MachineName,
                          UserName = registration.UserName
                      }).Entity;
        
        process.InstanceId = registration.InstanceId;
        process.ConnectionId = connectionId;
        process.LastOnline = DateTime.UtcNow;
        process.IsOnline = true;

        await _context.SaveChangesAsync();
        
        return new DiagProcess()
        {
            Id = process.Id,
            InstanceId = process.InstanceId,
            IsOnline = process.IsOnline,
            IsSending = process.IsSending,
            LastOnline = process.LastOnline,
            LastReceived = process.LastReceived,
            MachineName = process.MachineName,
            Name = process.Name,
            SiteId = process.SiteId
        };
    }

#endregion

    #region OnDisconnected => SIGNALR/connections/disconnected

    [Function("ProcessHub_OnProcessDisconnected")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<SignalRMessageAction?> OnDisconnected(
        [SignalRTrigger(PROCESS_HUB, "connections", "disconnected")]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        _logger.LogWarning($"----------------------------------- ClientTrigger.OnDisconnected {invokeContext.ConnectionId}");

        int siteId = GetSiteId(invokeContext);

        // await DiagIO.Process.SetConnectionId(processId, siteId, invokeContext.ConnectionId);
        var process = await _context.Processes.Where(p => p.SiteId == siteId && p.ConnectionId == invokeContext.ConnectionId)
            .FirstOrDefaultAsync() ?? throw new ApplicationException($"Can't find Process for ConnectionId {invokeContext.ConnectionId} for Site {siteId}");
        
        process.IsOnline = false;
        process.IsSending = false;
        process.ConnectionId = null;
        process.InstanceId = null;
        await _context.SaveChangesAsync();

        _logger.LogWarning($"OnDisconnected sending ReceiveProcess to GroupName {siteId}");
        return new SignalRMessageAction(nameof(IWebHubClient.ReceiveProcess), [process]) { GroupName = siteId.ToString() };
    }

    #endregion
    
   #region ReceiveDiagnostics => SIGNALR/ReceiveDiagnostics

    [Function("ProcessHub_ReceiveDiagnostics")]
    public async Task<DualHubOutput> ReceiveDiagnostics(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(IProcessHub.ReceiveDiagnostics), nameof(processId), nameof(stringData))]
        SignalRInvocationContext invokeContext,
        int processId,
        string stringData,
        FunctionContext context)
    {
        // DiagProcess process = await GetProcess(invokeContext);
        _logger.LogWarning($"ReceiveDiagnostics {processId} {stringData?.Substring(0, 20)}...");
        
        
        DualHubOutput output = new DualHubOutput();

        if (string.IsNullOrWhiteSpace(stringData))
            return output;

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

            output.WebClient.Add(new SignalRMessageAction(nameof(IWebHubClient.ReceiveDiagnostics), [processId, response])
            {
                GroupName = processId.ToString()
            });
        // }

        return output;
    }

    #endregion

    #region ClearEventStream => SIGNALR/ClearEventStream

    [Function("ProcessHub_ClearEvents")]
    [SignalROutput(HubName = WEB_HUB)]

    public async Task<SignalRMessageAction> ClearEvents(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(IProcessHub.ClearEvents), nameof(processId))]
        SignalRInvocationContext invokeContext,
        int processId,
        FunctionContext context)
    {
        // DiagProcess process = await GetProcess(invokeContext);
        _logger.LogWarning($"ClearEventStream");

        return new SignalRMessageAction(nameof(IWebHubClient.ClearEvents), [processId])
        {
            GroupName = processId.ToString()
        };
    }

    #endregion

    #region StreamEvents => SIGNALR/StreamEvents

    [Function("ProcessHub_StreamEvents")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<SignalRMessageAction> StreamEvents(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(IProcessHub.StreamEvents), nameof(processId), nameof(events))]
        SignalRInvocationContext invokeContext,
        int processId,
        SystemEvent[] events,
        FunctionContext context)
    {
        // DiagProcess process = await GetProcess(invokeContext);
        _logger.LogWarning($"StreamEvents");

        return new SignalRMessageAction(nameof(IWebHubClient.StreamEvents), [processId, events])
        {
            GroupName = processId.ToString()
        };
    }

    #endregion


    int GetSiteId(SignalRInvocationContext invokeContext)
    {
        if (!invokeContext.Claims.TryGetValue(SITE_ID_CLAIM, out var found))
            throw new ApplicationException($"Can't find SiteId claim");

        return int.Parse(found[0]!);
    }

 

}