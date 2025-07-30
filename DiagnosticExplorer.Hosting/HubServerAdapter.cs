using DiagnosticExplorer;
using DiagnosticExplorer.Util;
using Flurl.Http;
using log4net;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticExplorer.Hosting;

namespace DiagWebService.Hubs;

internal class HubServerAdapter : IDiagnosticHubClient
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(HubServerAdapter));
    private Task _writeEventTask;
    private Task _sendDiagnosticsTask;
    private TimeSpan _sendInterval = TimeSpan.FromSeconds(2);
    private CancellationTokenSource _sendDiagnosticsCancel;
    private CancellationTokenSource _writeEventCancel;
    public event EventHandler<RenewTimeEventArgs> RenewTimeChanged;
    private object _syncLock = new();
    

    private readonly HubConnection _hubConn;
    private readonly IFlurlClient _flurlClient;
    private readonly bool _isAzure;

    public HubServerAdapter(HubConnection hubConn, IFlurlClient flurlClient, bool isAzure)
    {
        _hubConn = hubConn ?? throw new ArgumentNullException(nameof(hubConn));
        _isAzure = isAzure;
        _flurlClient = flurlClient;

        _hubConn.On<string>(nameof(IDiagnosticHubClient.GetDiagnostics),
            async (requestId) => await GetDiagnostics(requestId));

        _hubConn.On<string, string, string>(nameof(IDiagnosticHubClient.SetProperty),
            async (requestId, context, value) => await SetProperty(requestId, context, value));

        _hubConn.On<string>("ReceiveMessage", msg => Trace.WriteLine($"***** ReceiveMessage {msg}"));

        // _hubConn.On<string>("NotificationReceived", msg => Trace.WriteLine($"***** NotificationReceived {msg}"));

        _hubConn.On<int>("StartSending", StartSending);
        _hubConn.On("StopSending", StopSending);

        _hubConn.On<int>("SetRenewTime",
            time =>
            {
                Trace.WriteLine($"Received renewTime {time}");
                RenewTimeChanged?.Invoke(this, new RenewTimeEventArgs(TimeSpan.FromSeconds(time)));
            });

        _hubConn.On<string, string, string, string[]>(nameof(IDiagnosticHubClient.ExecuteOperation),
            async (requestId, path, operation, args) => await ExecuteOperation(requestId, path, operation, args));

        _hubConn.On(nameof(IDiagnosticHubClient.SubscribeEvents),
            async () => await SubscribeEvents());

        _hubConn.On(nameof(IDiagnosticHubClient.UnsubscribeEvents),
            async () => await UnsubscribeEvents());
    }

    private void StopSending()
    {
        lock (_syncLock)
        {
            _sendDiagnosticsCancel?.Cancel();
            _sendDiagnosticsTask = null;

            UnsubscribeEvents();
        }
    }

    private void StartSending(int seconds)
    {
        lock (_syncLock)
        {
            Trace.WriteLine($"StartSending {seconds}");
            _sendInterval = TimeSpan.FromSeconds(seconds);

            if (_writeEventTask == null || _writeEventTask.IsCompleted)
                SubscribeEvents();

            if (_sendDiagnosticsTask == null || _sendDiagnosticsTask.IsCompleted)
            {
                Trace.WriteLine("########## START SENDING");
                CancellationTokenSource cancel = new CancellationTokenSource();
                _sendDiagnosticsTask = Task.Run(() => SendDiagnosticsLoop(cancel.Token), cancel.Token);
                _sendDiagnosticsCancel = cancel;
            }
        }
    }

    private async Task SendDiagnosticsLoop(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                DiagnosticResponse response = DiagnosticManager.GetDiagnostics();
                byte[] data = ProtobufUtil.Compress(response, 1024);
                string stringData = Convert.ToBase64String(data);
                Trace.WriteLine($"########## SendDiagnosticsLoop {data.Length} bytes verify {HashHelper.ComputeHashString(data)} data[0]: {data[0]}");

                await _hubConn.InvokeAsync(nameof(IDiagnosticHubServer.ReceiveDiagnostics), stringData, cancel);
            }
            catch when (cancel.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            try
            {
                await Task.Delay(_sendInterval, cancel);
            }
            catch {}
        }
        Trace.WriteLine("########## STOP SENDING");

    }

    public Task SubscribeEvents()
    {
        _writeEventCancel = new CancellationTokenSource();
        _writeEventTask = Task.Run(() => SendEventStream(_writeEventCancel.Token), _writeEventCancel.Token);
        return Task.CompletedTask;
    }

    public Task UnsubscribeEvents()
    {
        _writeEventCancel?.Cancel();
        _writeEventCancel = null;
        _writeEventTask = null;
        return Task.CompletedTask;
    }

    private async Task SendEventStream(CancellationToken cancel)
    {
        using EventSinkStream stream = EventSinkRepo.Default.CreateSinkStream(TimeSpan.FromMilliseconds(50), 100);
        //TODO harden this so it keeps trying if there is a failure
        try
        {
            await _hubConn.InvokeAsync(nameof(IDiagnosticHubServer.ClearEventStream), cancel);

            while (await stream.EventChannel.Reader.WaitToReadAsync(cancel))
            {
                IList<SystemEvent> item = await stream.EventChannel.Reader.ReadAsync(cancel);
                if (item.Any())
                    await _hubConn.InvokeAsync(nameof(IDiagnosticHubServer.StreamEvents), item, cancel);
            }
        }
        catch (OperationCanceledException)
        {
            Trace.WriteLine("HubServerAdapter.SendEventStream cancelled");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"HubServerAdapter.SendEventStream error: {ex}");
            
        }
    }

    public void Dispose()
    {
        UnsubscribeEvents();
        StopSending();
    }


    public Task GetDiagnostics(string requestId)
    {
        Task.Run(async () => {
            RpcResult<byte[]> result;
            try
            {
                DiagnosticResponse response = DiagnosticManager.GetDiagnostics();
                byte[] compress = ProtobufUtil.Compress(response, 1024);

                result = RpcResult<byte[]>.Success(requestId, compress);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                result = RpcResult<byte[]>.Fail(requestId, ex);
            }

            await _hubConn.InvokeAsync<string>(nameof(IDiagnosticHubServer.GetDiagnosticsReturn), result);
        });
        return Task.CompletedTask;
    }

    public Task SetProperty(string requestId, string path, string value)
    {
        Task.Run(async () => {
            RpcResult<OperationResponse> result = null;

            try
            {
                OperationResponse response = DiagnosticManager.SetProperty(path, value);
                result = RpcResult<OperationResponse>.Success(requestId, response);
            }
            catch (Exception ex)
            {
                result = RpcResult<OperationResponse>.Fail(requestId, ex);
            }
            finally
            {
                await _hubConn.InvokeAsync<string>(nameof(IDiagnosticHubServer.SetPropertyReturn), result);
            }
        });
        return Task.CompletedTask;
    }

    public Task ExecuteOperation(string requestId, string path, string operation, string[] args)
    {
        Task.Run(async () => {
            RpcResult<OperationResponse> result = null;

            try
            {
                OperationResponse response = DiagnosticManager.ExecuteOperation(path, operation, args);
                result = RpcResult<OperationResponse>.Success(requestId, response);
            }
            catch (Exception ex)
            {
                result = RpcResult<OperationResponse>.Fail(requestId, ex);
            }
            finally
            {
                await _hubConn.InvokeAsync<string>(nameof(IDiagnosticHubServer.ExecuteOperationReturn), result);
            }
        });
        return Task.CompletedTask;
    }

    public async Task Register(Registration registration)
    {
        await _hubConn.InvokeAsync(nameof(IDiagnosticHubServer.Register), registration);
    }

    public async Task Deregister(Registration registration)
    {
        if (_hubConn != null)
        {
            await _hubConn.InvokeAsync(nameof(IDiagnosticHubServer.Deregister), registration);
            
            Trace.WriteLine($"#################### Deregister");
        }
    }

    public async Task LogEvents(byte[] eventData)
    {
        RpcResult response;
        if (_isAzure)
            response = await _flurlClient.Request(nameof(IDiagnosticHubServer.LogEvents))
                .PostJsonAsync(eventData)
                .ReceiveJson<RpcResult>();
        else
            response = await _hubConn.InvokeAsync<RpcResult>(nameof(IDiagnosticHubServer.LogEvents), eventData);

        if (!response.IsSuccess)
            throw new ApplicationException(response.Message);
    }

} 