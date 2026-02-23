using Azure.Core.Serialization;
using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.DataExtensions;
using DiagnosticExplorer.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
                o.UseJsonObjectSerializer(new JsonObjectSerializer(DiagJsonOptions.Default));
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
        => await GetLoggedInAccount(int.Parse(context.UserId));

    #region OnConnected => SIGNALR/connections/connected
    
    [Function("WebHub_OnConnected")]
    public async Task OnConnected(
        [SignalRTrigger(WEB_HUB, "connections", "connected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        _logger.LogWarning($"HELLO FROM WEB_HUB.CONNECTED");

        _logger.LogWarning($"""
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

    [Function("WebHub_OnDisconnected")]
    public async Task OnDisconnected(
        [SignalRTrigger(WEB_HUB, "connections", "disconnected")] SignalRInvocationContext invocationContext, 
        FunctionContext context)
    {
        _logger.LogWarning($"----------------------------------- WebClient.OnDisconnected {invocationContext.ConnectionId}");

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
    public async Task SubscribeSite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhub/SubscribeSite")]
        HttpRequest req,
        string connectionId,
        int siteId)
    {
        _logger.LogWarning($"Connection {connectionId} SUBSCRIBE to {siteId}");

        Account account = await GetLoggedInAccount(req);
        await VerifySiteAccess(account, siteId);
        
        var hubContext = await _serviceManager.CreateHubContextAsync(WEB_HUB, CancellationToken.None);
        await hubContext.Groups.AddToGroupAsync(connectionId, siteId.ToString());
    }

    #endregion

    #region UnsubscribeSite => SIGNALR/UnsubscribeSite

    [Function("ClientHub_UnsubscribeSite")]
    public async Task UnsubscribeSite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhub/UnsubscribeSite")]
        HttpRequest req,
        string connectionId,
        string siteId)
    {
        _logger.LogWarning($"Connection {connectionId} UNSUBSCRIBE to {siteId}");

        var hubContext = await _serviceManager.CreateHubContextAsync(WEB_HUB, CancellationToken.None);
        await hubContext.Groups.RemoveFromGroupAsync(connectionId, siteId.ToString());
    }

    #endregion

    #region SubscribeProcess => POST /api/webhub/SubscribeProcess?processId=...

    [Function("ClientHub_SubscribeProcess")]
    public async Task<IActionResult> SubscribeProcess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhub/SubscribeProcess")]
        HttpRequest req,
        [FromQuery] string connectionId,
        [FromQuery] int processId)
    {
        var account = await GetLoggedInAccount(req);

        _logger.LogWarning($"SubscribeProcess accountId={account.Id} processId={processId} connectionId={connectionId}");

        var session = await _context.WebSessions
                          .Include(s => s.Subscriptions)
                          .FirstOrDefaultAsync(s => s.ConnectionId == connectionId)
                      ?? throw new ApplicationException($"WebSession not found for connection {connectionId}");

        var process = await _context.Processes
                          .Include(p => p.Subscriptions.Where(s => s.SessionId == session.Id))
                          .FirstOrDefaultAsync(p => p.Id == processId)
                      ?? throw new ApplicationException($"Can't find Process {processId}");

        await VerifySiteAccess(account, process.SiteId);

        process.IsSending = true;

        var sub = session.Subscriptions.FirstOrDefault(s => s.ProcessId == processId);
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

        ServiceHubContext webHubContext = await _serviceManager.CreateHubContextAsync(WEB_HUB, CancellationToken.None);
        await webHubContext.Groups.AddToGroupAsync(connectionId, processId.ToString());

        ServiceHubContext processHubContext = await _serviceManager.CreateHubContextAsync(PROCESS_HUB, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(process.ConnectionId))
        {
            await processHubContext.Clients.Client(process.ConnectionId)
                .SendCoreAsync(nameof(IProcessHubClient.StartSending), [DIAG_SEND_FREQ_MILLIS]);

            await processHubContext.Clients.Client(process.ConnectionId)
                .SendAsync(nameof(IProcessHubClient.ReceiveMessage), "################## Start sending please");
        }

        return new OkResult();
    }

    #endregion

    #region UnsubscribeProcess => SIGNALR/UnsubscribeProcess

    [Function("ClientHub_UnsubscribeProcess")]
    public async Task UnsubscribeProcess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhub/UnsubscribeProcess")]
        HttpRequest req,
        [FromQuery] string connectionId,
        [FromQuery] int processId)
    {
        _logger.LogWarning($"Connection {connectionId} UNSUBSCRIBE to {processId}");

       var session = await _context.WebSessions
            .FirstOrDefaultAsync(s => s.ConnectionId == connectionId)
            ?? throw new ApplicationException($"WebSession not found for connection {connectionId}");
        
        var process = await _context.Processes
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == processId)
                              ?? throw new ApplicationException($"Can't find Process {processId}");

        
        process.Subscriptions.RemoveAll(s => s.SessionId == session.Id);
        await _context.SaveChangesAsync();

        ServiceHubContext webHubContext = await _serviceManager.CreateHubContextAsync(WEB_HUB, CancellationToken.None);

        await webHubContext.Groups.RemoveFromGroupAsync(connectionId, processId.ToString());
        
        if (!string.IsNullOrWhiteSpace(process.ConnectionId) && process.Subscriptions.Count == 0)
        {
            ServiceHubContext processHubContext = await _serviceManager.CreateHubContextAsync(PROCESS_HUB, CancellationToken.None);
            await processHubContext.Clients.Client(process.ConnectionId).SendCoreAsync(nameof(IProcessHubClient.StopSending), [], CancellationToken.None);
            await processHubContext.Clients.Client(process.ConnectionId)
                .SendAsync(nameof(IProcessHubClient.ReceiveMessage), "################## Stop sending please");
        }
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

        return new SignalRMessageAction(nameof(IProcessHubClient.SetProperty), ["RequestId (Ignore)", request.Path, request.Value ?? ""])
        {
            ConnectionId = process.ConnectionId
        };
    }

    #endregion
    
    #region SetProperty => SIGNALR/ExecuteOperation

    [Function("ProcessHub_ExecuteOperation")]
    [SignalROutput(HubName = PROCESS_HUB)]
    public async Task<SignalRMessageAction> ExecuteOperation(
        [SignalRTrigger(WEB_HUB, MESSAGES, nameof(ExecuteOperation), nameof(request))]
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

        return new SignalRMessageAction(nameof(IProcessHubClient.SetProperty), ["RequestId (Ignore)", request.Path, request.Value ?? ""])
        {
            ConnectionId = process.ConnectionId
        };
    }

    #endregion
   
}