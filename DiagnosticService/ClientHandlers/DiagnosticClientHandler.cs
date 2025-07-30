﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticExplorer;
using DiagnosticExplorer.Util;
using Diagnostics.Service.Common.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DiagWebService.ClientHandlers;

public class DiagnosticClientHandler : HubProxyBase, IDiagnosticClient
{
    private readonly IDiagnosticHubClient _client;
    private readonly HubCallerContext _callerContext;
    public event EventHandler Disconnected;
    public Subject<SystemEvent[]> EventsSet { get; } = new();
    public Subject<SystemEvent[]> EventsStreamed { get; } = new();

    public DiagnosticClientHandler(HubCallerContext callerContext, IDiagnosticHubClient client, AsyncResultBucket responses)
        : base(responses)
    {
        _client = client;
        _callerContext = callerContext;
        ConnectionId = callerContext.ConnectionId;
        _callerContext.ConnectionAborted.Register(() => Disconnected?.Invoke(this, EventArgs.Empty));
    }

    public string ConnectionId { get; }

    public async Task<DiagnosticResponse> GetDiagnostics(CancellationToken cancel)
    {
        byte[] data = await SendRequest<byte[]>(cancel, requestId => _client.GetDiagnostics(requestId));
        return ProtobufUtil.Decompress<DiagnosticResponse>(data);
    }

    public Task<OperationResponse> SetProperty(string path, string value)
    {
        return SendRequest<OperationResponse>(CancellationToken.None, requestId => _client.SetProperty(requestId, path, value));
    }

    public Task<OperationResponse> ExecuteOperation(string path, string operation, string[] arguments)
    {
        return SendRequest<OperationResponse>(CancellationToken.None, requestId => _client.ExecuteOperation(requestId, path, operation, arguments));
    }

    public async Task SubscribeEvents()
    {
        await _client.SubscribeEvents();
    }

    public async Task UnsubscribeEvents()
    {
        await _client.UnsubscribeEvents();
    }

    public void SetEvents(SystemEvent[] events)
    {
        EventsSet.OnNext(events);
    }

    public void StreamEvents(SystemEvent[] evt)
    {
        EventsStreamed.OnNext(evt);
    }

    public void CloseConnection()
    {
        _callerContext.Abort();
    }
}

