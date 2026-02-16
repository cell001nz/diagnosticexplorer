using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.DataExtensions;
using DiagnosticExplorer.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.SignalR.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.Endpoints;

public class WebHubApi : ApiBase
{
    private readonly ServiceManager _serviceManager;

    public WebHubApi(ILogger<WebHubApi> logger, DiagDbContext context) : base(logger, context)
    {
         _serviceManager = new ServiceManagerBuilder()
            .WithOptions(o =>
            {
                o.ConnectionString = Environment.GetEnvironmentVariable("AzureSignalRConnectionString");
            }).BuildServiceManager();
    }

    #region Negotiate => GET /api/webclient/negotiate

    [Function("WebHub_negotiate")]
    public async Task<IActionResult> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "webhub/negotiate")]
        HttpRequest req)
    {
        _logger.LogWarning($"### HEADERS ### {string.Join(", ", req.Headers.Keys)}");
        // return new OkObjectResult(connectionInfo);

        Account account = await GetLoggedInAccount(req);

        var hubContext = await _serviceManager.CreateHubContextAsync(WEB_HUB, CancellationToken.None);
        NegotiationResponse negResponse = await hubContext.NegotiateAsync(new NegotiationOptions()
                                          {
                                              UserId = account.Id.ToString(),
                                          })
                                          ?? throw new ApplicationException("Failed to negotiate SignalR connection");

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

    #endregion
    
    protected async Task<Account> GetLoggedInAccount(SignalRInvocationContext context)
    {
        int userId = int.Parse(context.UserId);
        return await _context.Accounts.Where(a => a.Id == userId)
            .Select(AccountEntityUtil.Projection)
            .FirstOrDefaultAsync()
            ?? throw new ApplicationException("Current user not found");
    }
    
    #region OnConnected => SIGNALR/connections/connected
    
    [Function("WebHub_OnClientConnected")]
    public async Task OnConnected(
        [SignalRTrigger(WEB_HUB, "connections", "connected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OnConnected));
        logger.LogInformation($"""
                               ----------------------------------- WebClient.OnConnected 
                                   ConnectionId {invocationContext.ConnectionId}
                                   UserId {invocationContext.UserId}
                               """);
        
        Account account = await GetLoggedInAccount(invocationContext);

        _context.WebSessions.Add(new WebSessionEntity()
        {
            ConnectionId = invocationContext.ConnectionId,
            AccountId = account.Id,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        await _context.SaveChangesAsync();
    }

    #endregion

    #region OnDisconnected => SIGNALR/connections/disconnected

    [Function("WebHub_OnClientDisconnected")]
    public async Task OnDisconnected(
        [SignalRTrigger(WEB_HUB, "connections", "disconnected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        _logger.LogInformation($"----------------------------------- WebClient.OnDisconnected {invocationContext.ConnectionId}");

        WebSessionEntity session = await _context.WebSessions
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(s => s.ConnectionId == invocationContext.ConnectionId)
            ?? throw new ApplicationException($"WebSession not found for connection {invocationContext.ConnectionId}");

        session.EndedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
    
    #endregion
    
    #region SubscribeSite => SIGNALR/SubscribeSite

    [Function("ClientHub_SubscribeSite")]
    [SignalROutput(HubName = WEB_HUB)]
    public async Task<object[]> SubscribeSite(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(SubscribeSite), nameof(siteId))]
        SignalRInvocationContext invokeContext,
        int siteId,
        FunctionContext context)
    {
        var logger = context.GetLogger("ClientHub_SubscribeSite");
        logger.LogWarning($"Connection {invokeContext.ConnectionId} SUBSCRIBE to {siteId}");

        Account account = await GetLoggedInAccount(invokeContext);
        await VerifySiteAccess(account, siteId);
        
        return
        [
            new SignalRGroupAction(SignalRGroupActionType.Add)
            {
                GroupName = siteId.ToString(),
                ConnectionId = invokeContext.ConnectionId
            },
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
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(SubscribeProcess), nameof(processId))]
        SignalRInvocationContext invokeContext,
        int processId,
        FunctionContext context)
    {
        _logger.LogWarning($"Connection {invokeContext.ConnectionId} SUBSCRIBE to {processId}");

        var session = await _context.WebSessions
                          .Include(s => s.Subscriptions)
                          .FirstOrDefaultAsync(s => s.ConnectionId == invokeContext.ConnectionId)
                      ?? throw new ApplicationException($"WebSession not found for connection {invokeContext.ConnectionId}");

        var process = await _context.Processes
                          .Include(p => p.Subscriptions.Where(s => s.SessionId == session.Id))
                          .FirstOrDefaultAsync(p => p.Id == processId)
                      ?? throw new ApplicationException($"Can't find Process {processId}");
        
        Account account = await GetLoggedInAccount(invokeContext);
        await VerifySiteAccess(account, process.SiteId);

        DualHubOutput output = new();

        //If the process is not sending already, instruct it to start sending diagnostics
        process.IsSending = true;
        
        var sub = session.Subscriptions.FirstOrDefault(sub => sub.ProcessId == processId);
        if (sub == null)
        {
            sub = new WebSubcriptionEntity()
            {
                ProcessId = processId,
                SessionId = session.Id,
                CreatedAt = DateTime.UtcNow
            };
            session.Subscriptions.Add(sub);
        }
        sub.RenewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        output.ProcessClient.Add(new SignalRMessageAction(nameof(IProcessHubClient.StartSending), [DIAG_SEND_FREQ_MILLIS])
            { ConnectionId = process.ConnectionId });
        
        output.WebClient.Add(new SignalRGroupAction(SignalRGroupActionType.Add)
        {
            GroupName = processId.ToString(),
            ConnectionId = invokeContext.ConnectionId
        });
        
        return output;
    }

    #endregion

    #region UnsubscribeProcess => SIGNALR/UnsubscribeProcess

    [Function("ClientHub_UnsubscribeProcess")]
    public async Task<DualHubOutput> UnsubscribeProcess(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(UnsubscribeProcess), nameof(processId))]
        SignalRInvocationContext invokeContext,
        int processId,
        FunctionContext context)
    {
        _logger.LogWarning($"Connection {invokeContext.ConnectionId} UNSUBSCRIBE to {processId}");

       var session = await _context.WebSessions
            .FirstOrDefaultAsync(s => s.ConnectionId == invokeContext.ConnectionId)
            ?? throw new ApplicationException($"WebSession not found for connection {invokeContext.ConnectionId}");
        
        var process = await _context.Processes
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == processId)
                              ?? throw new ApplicationException($"Can't find Process {processId}");

        
        process.Subscriptions.RemoveAll(s => s.SessionId == session.Id);
        await _context.SaveChangesAsync();
        DualHubOutput output = new();

        output.WebClient.Add(new SignalRGroupAction(SignalRGroupActionType.Remove) 
            { ConnectionId = invokeContext.ConnectionId});

        if (process.Subscriptions.Count == 0)
        {
            output.ProcessClient.Add(new SignalRMessageAction(nameof(IProcessHubClient.StopSending)) { ConnectionId = process.ConnectionId });
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
        if (string.IsNullOrWhiteSpace(request.Path))
            throw new ApplicationException("Path is required");
        
        var process = await _context.Processes
                          .FirstOrDefaultAsync(p => p.Id == request.ProcessId)
                      ?? throw new ApplicationException($"Can't find Process {request.ProcessId}");
        
        await VerifySiteAccess(await GetLoggedInAccount(invokeContext), process.SiteId);

        if (string.IsNullOrWhiteSpace(process.ConnectionId))
            throw new ApplicationException($"Process {request.ProcessId} is not connected");

        return new SignalRMessageAction("SetProperty", ["RequestId (Ignore)", request.Path, request.Value ?? ""])
        {
            ConnectionId = process.ConnectionId
        };
    }

    #endregion
   
}