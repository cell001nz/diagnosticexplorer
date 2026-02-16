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
    private const string PROCESS_CLAIM = "DiagProcessClaim";
    
    private readonly ServiceManager _serviceManager;
    
    public ProcessHubApi(ILogger<ProcessHubApi> logger, DiagDbContext context) : base(logger, context)
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
        HttpRequest req,
        [FromBody] Registration registration)
    {
        _logger.LogWarning($"----------------------------------- ProcessHubTrigger.Negotiate {registration?.InstanceId} {registration?.ProcessName}");

        if (registration?.Code == null || registration.Secret == null)
        {
            _logger.LogWarning($"----------------------------------- Missing clientId or secret");
            return new BadRequestObjectResult("Missing clientId or secret");
        }
        
        DiagProcess process = await RegisterProcess(registration);
        List<Claim> customClaims =
        [
            new Claim(PROCESS_CLAIM, $"{process.Id}/{process.SiteId}")
        ];

        var hubContext = await _serviceManager.CreateHubContextAsync(PROCESS_HUB, CancellationToken.None);
        NegotiationResponse negResponse = await hubContext.NegotiateAsync(new NegotiationOptions()
                                    {
                                        UserId = $"{process.Id}/{process.SiteId}",
                                        Claims = customClaims
                                    }) ??
                                    throw new ApplicationException("Failed to negotiate SignalR connection");
        
        if (string.IsNullOrWhiteSpace(negResponse.Url))
            throw new ApplicationException("Failed to negotiate SignalR connection - Url is null or empty");
    
        if (string.IsNullOrWhiteSpace(negResponse.AccessToken))
            throw new ApplicationException("Failed to negotiate SignalR connection - AccessToken is null or empty");
    
        SignalRConnectionInfo info = new SignalRConnectionInfo()
        {
            Url = negResponse.Url,
            AccessToken = negResponse.AccessToken
        };
        
        // Console.WriteLine($"ConnectionInfo is null: {connectionInfo?.Url} {connectionInfo?.AccessToken}");
        return new OkObjectResult(info);
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
    
    #region OnConnected => SIGNALR/connections/connected

    [Function("OnClientConnected")]
    public async Task<DualHubOutput> OnConnected(
        [SignalRTrigger(PROCESS_HUB, "connections", "connected")]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        var (processId, siteId) = GetProcessAndSiteId(invokeContext);
        _logger.LogWarning($"----------------------------------- ProcessHubTrigger.OnConnected {invokeContext.ConnectionId}/{processId}/{siteId}");

        
        // await DiagIO.Process.SetConnectionId(processId, siteId, invokeContext.ConnectionId);
        var process = await _context.Processes.Where(p => p.Id == processId)
            .Select(ProcessEntityUtil.Projection)
            .FirstOrDefaultAsync() ?? throw new ApplicationException($"Can't find Process {processId} for Site {siteId}");

        DualHubOutput output = new DualHubOutput();
        output.WebClient.Add(new SignalRMessageAction(Messages.Web.ReceiveProcess, [process]) { GroupName = siteId.ToString() });

        output.ProcessClient.Add(new SignalRMessageAction(Messages.Process.SetRenewTime, [PROCESS_RENEW_TIME])
        {
            ConnectionId = invokeContext.ConnectionId
        });

        int subs = await _context.Processes.Where(p => p.Id == processId)
            .Select(p => p.Subscriptions.Count)
            .FirstOrDefaultAsync();
        
        if (subs > 0)
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
        [SignalRTrigger(PROCESS_HUB, "connections", "disconnected")]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        _logger.LogWarning($"----------------------------------- ClientTrigger.OnDisconnected {invokeContext.ConnectionId}");

        var (processId, siteId) = GetProcessAndSiteId(invokeContext);

        // await DiagIO.Process.SetConnectionId(processId, siteId, invokeContext.ConnectionId);
        var process = await _context.Processes.Where(p => p.Id == processId)
            .FirstOrDefaultAsync() ?? throw new ApplicationException($"Can't find Process {processId} for Site {siteId}");
        process.IsOnline = false;
        process.IsSending = false;
        process.ConnectionId = null;
        process.InstanceId = null;
        await _context.SaveChangesAsync();

        _logger.LogWarning($"OnDisconnected sending ReceiveProcess to GroupName {siteId}");
        return new SignalRMessageAction("ReceiveProcess", [process]) { GroupName = siteId.ToString() };
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
        _logger.LogWarning($"ReceiveDiagnostics {stringData}");
        DualHubOutput output = new DualHubOutput();
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
                GroupName = processId.ToString()
            });
        // }

        return output;
    }

    #endregion

    #region ClearEventStream => SIGNALR/ClearEventStream

    [Function("ProcessHub_ClearEventStream")]
    [SignalROutput(HubName = WEB_HUB)]

    public async Task<SignalRMessageAction> ClearEventStream(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(ClearEventStream))]
        SignalRInvocationContext invokeContext,
        FunctionContext context)
    {
        // DiagProcess process = await GetProcess(invokeContext);
        _logger.LogWarning($"ClearEventStream");
        var (processId, siteId) = GetProcessAndSiteId(invokeContext);

        return new SignalRMessageAction(Messages.Web.ClearEventStream, [processId])
        {
            GroupName = processId.ToString()
        };
    }

    #endregion

    #region StreamEvents => SIGNALR/StreamEvents

    [Function("ProcessHub_StreamEvents")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<SignalRMessageAction> StreamEvents(
        [SignalRTrigger(PROCESS_HUB, MESSAGES, nameof(StreamEvents), nameof(events))]
        SignalRInvocationContext invokeContext,
        SystemEvent[] events,
        FunctionContext context)
    {
        // DiagProcess process = await GetProcess(invokeContext);
        _logger.LogWarning($"StreamEvents");

        var (processId, siteId) = GetProcessAndSiteId(invokeContext);

        return new SignalRMessageAction(Messages.Web.StreamEvents, [processId, events])
        {
            GroupName = processId.ToString()
        };
    }

    #endregion

    #region RegisterProcess

    private async Task<DiagProcess> RegisterProcess(Registration registration)
    {
        PasswordHasher hasher = new PasswordHasher();
        var secretHash = hasher.HashSecret(registration.Secret);

        Expression<Func<ProcessEntity, bool>> processFilter = p => p.InstanceId == registration.InstanceId
                                                                   || (p.MachineName == registration.MachineName && p.UserName == registration.UserName && p.Name == registration.ProcessName);

        var site = await _context.Sites
            .Where(s => s.Code == registration.Code && s.Secrets.Any(secret => secret.Hash == secretHash))
            .Include(s => s.Processes.AsQueryable().Where(processFilter))
            .FirstOrDefaultAsync() ?? throw new ApplicationException("Invalid registration code or secret");

        var process = site.Processes.FirstOrDefault(p => p.InstanceId == registration.InstanceId)
                      ?? site.Processes.FirstOrDefault(p => !p.IsOnline)
                      ?? site.Processes.FirstOrDefault(p => DateTime.UtcNow - p.LastOnline > TimeSpan.FromSeconds(PROCESS_STALE_TIME));
        if (process == null)
        {
            process = new ProcessEntity()
            {
                Name = registration.ProcessName,
                MachineName = registration.MachineName,
                UserName = registration.UserName
            };
            site.Processes.Add(process);
        }

        process.InstanceId = registration.InstanceId;
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
  
    (int processId, int siteId) GetProcessAndSiteId(SignalRInvocationContext invokeContext)
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
     
        int processId = int.Parse(parts[0]);
        int siteId = int.Parse(parts[1]);
        return (processId, siteId);
    }

}