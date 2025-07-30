﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DiagnosticExplorer;
using DiagnosticExplorer.Util;
using DiagWebService.ClientHandlers;
using log4net;
using Microsoft.AspNetCore.SignalR;

namespace Diagnostics.Service.Common.Hubs;

public class DiagnosticHub : Hub<IDiagnosticHubClient>, IDiagnosticHubServer
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(DiagnosticHub));
    private readonly RealtimeManager _rtManager;
    private readonly RetroManager _retroManager;
    private static readonly AsyncResultBucket _clientResponses = new();

    public DiagnosticHub(RealtimeManager rtManager, RetroManager retroManager)
    {
        _rtManager = rtManager;
        _retroManager = retroManager;
    }

    public override Task OnConnectedAsync()
    {
        _rtManager.AddDiagnosticClient(new DiagnosticClientHandler(Context, Clients.Caller, _clientResponses));
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        Trace.WriteLine("Disconnected");
        Trace.WriteLine(ex);
        return base.OnDisconnectedAsync(ex);
    }

    public async Task<RpcResult<RegistrationResponse>> Register(Registration registration)
    {
        RegistrationResponse response = new(TimeSpan.FromSeconds(20));
        try
        {
            _rtManager.Register(registration, Context.ConnectionId);
            return RpcResult<RegistrationResponse>.Success(response);

            // return Clients.Caller.RegisterReturn(RpcResult.Success(requestId), response);
        }
        catch (Exception ex)
        {
            return RpcResult<RegistrationResponse>.Fail(requestId: null, ex.Message, ex.ToString());
        }
    }

    public Task<RpcResult> Deregister(Registration registration)
    {
        try
        {
            _rtManager.Deregister(registration);
            return Task.FromResult(RpcResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(RpcResult.Fail(requestId: null, ex));
        }
    }

    public async Task<RpcResult> LogEvents(byte[] eventData)
    {
        try
        {
            DiagnosticMsg[]? messages = ProtobufUtil.Decompress<DiagnosticMsg[]>(eventData);
            if (messages?.Any() == true)
            {
                 _rtManager.RegisterAlertLevel(Context.ConnectionId, messages);

                _retroManager.LogEvents(messages);
            }

            return RpcResult.Success();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            return RpcResult.Fail(requestId: null, ex);
        }
    }

    public Task GetDiagnosticsReturn(RpcResult<byte[]> response)
    {
        _clientResponses.SetResult(response, response.Response);
        return Task.CompletedTask;
    }

    public Task ReceiveDiagnostics(byte[] response)
    {
        _clientResponses.SetResult(RpcResult.Success(), response);
        return Task.CompletedTask;
    }

    public Task ExecuteOperationReturn(RpcResult<OperationResponse> response)
    {
        _clientResponses.SetResult(response, response.Response);
        return Task.CompletedTask;
    }

    public Task SetPropertyReturn(RpcResult<OperationResponse> response)
    {
        _clientResponses.SetResult(response, response.Response);
        return Task.CompletedTask;
    }

    public Task ClearEventStream()
    {
        var client = _rtManager.GetClientHandler(Context.ConnectionId);
        client.SetEvents([]);
        return Task.CompletedTask;
    }

    public Task SetEvents(SystemEvent[] events)
    {
        var client = _rtManager.GetClientHandler(Context.ConnectionId);
        client.SetEvents(events);
        return Task.CompletedTask;
    }

    public Task StreamEvents(SystemEvent[] evts)
    {
        var client = _rtManager.GetClientHandler(Context.ConnectionId);
        client.StreamEvents(evts);
        return Task.CompletedTask;
    }

    public Task StreamEvents(byte[] eventData)
    {
        var client = _rtManager.GetClientHandler(Context.ConnectionId);
        SystemEvent[] events = ProtobufUtil.Decompress<SystemEvent[]>(eventData);
        client.StreamEvents(events);
        return Task.CompletedTask;
    }
}