﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using DiagnosticExplorer;
using Diagnostics.Service.Common.Transport;
using log4net;
using Microsoft.AspNetCore.SignalR;

namespace Diagnostics.Service.Common.Hubs;

public class WebHub : Hub<IWebHubClient>
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(WebHub));
    private readonly RealtimeManager _realtimeManager;
    private readonly RetroManager _retroManager;

    public WebHub(RealtimeManager realtimeManager, RetroManager retroManager)
    {
        _realtimeManager = realtimeManager;
        _retroManager = retroManager;
    }

    public override async Task OnConnectedAsync()
    {
        Debug.WriteLine($"WebHub OnConnectedAsync {Context.ConnectionId}");
        await base.OnConnectedAsync();
        _realtimeManager.AddWebHubClient(Context.ConnectionId, Clients.Caller);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Debug.WriteLine($"WebHub OnDisconnectedAsync {exception}");
        _realtimeManager.RemoveWebHubClient(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task Subscribe(string processId)
    {
        await _realtimeManager.SubscribeWebClient(Context.ConnectionId, processId);
    }

    public Task RemoveProcess(string processId)
    {
        _realtimeManager.RemoveProcess(processId);
        return Task.CompletedTask;
    }

    public async Task<OperationResponse> SetProperty(SetPropertyRequest request)
    {
        return await _realtimeManager.SetProperty(request);
    }

    public async Task<OperationResponse> ExecuteOperation(ExecuteOperationRequest request)
    {
        return await _realtimeManager.ExecuteOperation(request);
    }

    public Task StartRetroSearch(RetroQuery query)
    {
        return _retroManager.StartRetroSearch(query, Context.ConnectionId, Clients.Caller);
    }

    public Task<long> RetroDelete(string[] recordList)
    {
        return _retroManager.RetroDelete(recordList);
    }

    public Task CancelRetroSearch(int searchId)
    {
        return _retroManager.CancelRetroSearch(searchId, Context.ConnectionId);
    }
}