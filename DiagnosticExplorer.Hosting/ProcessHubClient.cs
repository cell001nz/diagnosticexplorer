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
using DiagnosticExplorer.Domain;
using DiagnosticExplorer.Hosting;

namespace DiagWebService.Hubs;

internal class ProcessHubClient : IProcessHubClient
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(ProcessHubClient));
    private static readonly string _instanceId = Guid.NewGuid().ToString("N");
    private Task _writeEventTask;
    private Task _sendDiagnosticsTask;
    private int _processId = 0;
    private TimeSpan _sendInterval = TimeSpan.FromSeconds(2);
    private CancellationTokenSource _sendDiagnosticsCancel;
    private CancellationTokenSource _writeEventCancel;
    private readonly AutoResetEvent _sendNowEvent = new(false);
    public event EventHandler<RenewTimeEventArgs> RenewTimeChanged;
    private object _syncLock = new();


    private readonly HubConnection _hubConn;
    private readonly IFlurlClient _flurlClient;

    public ProcessHubClient(HubConnection hubConn, IFlurlClient flurlClient)
    {
        _hubConn = hubConn ?? throw new ArgumentNullException(nameof(hubConn));
        _flurlClient = flurlClient;

        _hubConn.On(nameof(IProcessHubClient.SetProperty), async (string requestId, string context, string value) => await ((IProcessHubClient)this).SetProperty(requestId, context, value));
        _hubConn.On(nameof(IProcessHubClient.ReceiveMessage), async (string msg) => await ((IProcessHubClient)this).ReceiveMessage(msg));
        _hubConn.On(nameof(IProcessHubClient.SetProcessId), async (int processId) => await ((IProcessHubClient)this).SetProcessId(processId));
        _hubConn.On(nameof(IProcessHubClient.StartSending), async (int millis) => await ((IProcessHubClient)this).StartSending(millis));
        _hubConn.On(nameof(IProcessHubClient.StopSending), async () => await ((IProcessHubClient)this).StopSending());
        _hubConn.On(nameof(IProcessHubClient.SetRenewTime), async (int millis) => await ((IProcessHubClient)this).SetRenewTime(millis));
        _hubConn.On(nameof(IProcessHubClient.ExecuteOperation),
            async (string requestId, string path, string operation, string[] args) => await ((IProcessHubClient)this).ExecuteOperation(requestId, path, operation, args));
    }

    public async Task Register(CancellationToken cancel)
    {
        var registration = new Registration
        {
            Pid = Process.GetCurrentProcess().Id,
            InstanceId = _instanceId,
            UserName = Environment.UserName,
            MachineName = Environment.MachineName,
            ProcessName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "")
        };
        await _hubConn.InvokeAsync(nameof(IProcessHub.Register), registration, cancel);
    }

    Task IProcessHubClient.SetRenewTime(int millis)
    {
        RenewTimeChanged?.Invoke(this, new RenewTimeEventArgs(TimeSpan.FromMilliseconds(millis)));
        return Task.CompletedTask;
    }

    Task IProcessHubClient.ReceiveMessage(string message)
    {
        Trace.WriteLine($"***** ReceiveMessage {message}");
        return Task.CompletedTask;
    }

    Task IProcessHubClient.SetProcessId(int processId)
    {
        _processId = processId;
        return Task.CompletedTask;
    }

    Task IProcessHubClient.StartSending(int millis)
    {
        Trace.WriteLine($"START SENDING PID {Process.GetCurrentProcess().Id} ({millis} ms)");
        lock (_syncLock)
        {
            _sendInterval = TimeSpan.FromMilliseconds(millis);
            if (_writeEventTask == null || _writeEventTask.IsCompleted)
                SubscribeEvents();

            if (_sendDiagnosticsTask == null || _sendDiagnosticsTask.IsCompleted)
            {
                var cancel = new CancellationTokenSource();
                _sendDiagnosticsTask = Task.Run(() => SendDiagnosticsLoop(cancel.Token), cancel.Token);
                _sendDiagnosticsCancel = cancel;
            }
            else
            {
                // Signal the loop to send immediately
                _sendNowEvent.Set();
            }
        }

        return Task.CompletedTask;
    }

    Task IProcessHubClient.StopSending()
    {
        StopSending();
        return Task.CompletedTask;
    }

    public void StopSending()
    {
        lock (_syncLock)
        {
            Trace.WriteLine($"StopSending {GetHashCode()} {_sendDiagnosticsCancel?.GetHashCode()}");
            _sendDiagnosticsCancel?.Cancel();
            _sendDiagnosticsTask = null;

            UnsubscribeEvents();
        }
    }

    private async Task SendDiagnosticsLoop(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            Trace.WriteLine($"SendDiagnosticsLoop {GetHashCode()} {cancel.GetHashCode()}/{_sendDiagnosticsCancel?.GetHashCode()}");

            try
            {
                var response = DiagnosticManager.GetDiagnostics();
                var data = ProtobufUtil.Compress(response, 1024);
                var stringData = Convert.ToBase64String(data);
                Trace.WriteLine($"SENDING DIAGNOSTICS {Process.GetCurrentProcess().Id}");
                await _hubConn.InvokeAsync(nameof(IDiagnosticHubServer.ReceiveDiagnostics), _processId, stringData, cancel);
            }
            catch when (cancel.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            // Wait on the AutoResetEvent with timeout, allowing immediate trigger via Set()
            try
            {
                await Task.Run(() => _sendNowEvent.WaitOne(_sendInterval), cancel);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void SubscribeEvents()
    {
        _writeEventCancel = new CancellationTokenSource();
        _writeEventTask = Task.Run(() => SendEventStream(_writeEventCancel.Token), _writeEventCancel.Token);
    }

    private void UnsubscribeEvents()
    {
        _writeEventCancel?.Cancel();
        _writeEventCancel = null;
        _writeEventTask = null;
    }

    private async Task SendEventStream(CancellationToken cancel)
    {
        using var stream = EventSinkRepo.Default.CreateSinkStream(TimeSpan.FromMilliseconds(50), 100);
        try
        {
            await _hubConn.InvokeAsync(nameof(IProcessHub.ClearEvents), _processId, cancel);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"HubServerAdapter.ClearEvents error: {ex}");
        }

        try
        {
            while (await stream.EventChannel.Reader.WaitToReadAsync(cancel))
            {
                var item = await stream.EventChannel.Reader.ReadAsync(cancel);
                if (item.Any())
                    await _hubConn.InvokeAsync(nameof(IProcessHub.StreamEvents), _processId, item, cancel);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"HubServerAdapter.SendEventStream error: {ex}");
        }
    }

    Task IProcessHubClient.SetProperty(string requestId, string path, string value)
    {
        Task.Run(async () =>
        {
            RpcResult<OperationResponse> result = null;

            try
            {
                var response = DiagnosticManager.SetProperty(path, value);
                // result = RpcResult<OperationResponse>.Success(requestId, response);
            }
            catch (Exception ex)
            {
                // result = RpcResult<OperationResponse>.Fail(requestId, ex);
            }
        });
        return Task.CompletedTask;
    }

    Task IProcessHubClient.ExecuteOperation(string requestId, string path, string operation, string[] args)
    {
        Task.Run(async () =>
        {
            // RpcResult<OperationResponse> result = null;

            try
            {
                var response = DiagnosticManager.ExecuteOperation(path, operation, args);
                // result = RpcResult<OperationResponse>.Success(requestId, response);
            }
            catch (Exception ex)
            {
                // result = RpcResult<OperationResponse>.Fail(requestId, ex);
            }
            finally
            {
                // await _hubConn.InvokeAsync<string>(nameof(IDiagnosticHubServer.ExecuteOperationReturn), result);
            }
        });
        return Task.CompletedTask;
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
        var response = await _flurlClient.Request(nameof(IDiagnosticHubServer.LogEvents))
            .PostJsonAsync(eventData)
            .ReceiveJson<RpcResult>();

        if (!response.IsSuccess)
            throw new ApplicationException(response.Message);
    }
}