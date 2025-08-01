using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticExplorer.Util;
using DiagWebService.Hubs;
using Flurl.Http;
using Flurl.Http.Configuration;
using log4net;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;


namespace DiagnosticExplorer;

public class RegistrationHandler
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(RegistrationHandler));

    private DiagnosticSite _site;
    private string _resolvedUrl;
    private Registration _registration;
    private HubServerAdapter _hubAdapter;
    private HubConnection _connection;
    private TimeSpan _renewTime = TimeSpan.FromSeconds(25);

    private CancellationTokenSource _stopToken;
    private Task _registrationLoop;
    private Task _loggingTask;
    private Subject<DiagnosticMsg> _logSubject = new();
    private Channel<IList<DiagnosticMsg>> _logChannel;
    private readonly IFlurlClientCache _flurlCache;
    private static readonly string _instanceId = Guid.NewGuid().ToString("N");

    public RegistrationHandler(DiagnosticSite site, IFlurlClientCache flurlCache)
    {
        _site = site;
        _resolvedUrl = site.Url;
        _flurlCache = flurlCache;
        
        _registration = new() {
            Code = site.Code,
            Secret = site.Secret,
            ProcessId = Process.GetCurrentProcess().Id,
            InstanceId = _instanceId,
            UserName = Environment.UserName,
            MachineName = Environment.MachineName,
            ProcessName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "")
        };
    }

    public void Start()
    {
        _stopToken = new CancellationTokenSource();
        _logChannel = Channel.CreateBounded<IList<DiagnosticMsg>>(
            new BoundedChannelOptions(1_000_000) {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

        _logSubject
            .Buffer(TimeSpan.FromSeconds(2), 50)
            .Where(evts => evts.Count != 0)
            .Subscribe(evts => _logChannel?.Writer.TryWrite(evts));

        
        _registrationLoop = Task.Run(() => RunRegistrationProcess(_stopToken.Token));
        _loggingTask = Task.Run(() => RunLoggingProcess(_stopToken.Token));

        Debug.WriteLine($"Diagnostics RegistrationHandler for {UrlInfo} started");
    }

    //TODO: Tighten this up to handle failure and cancellation better
    private async Task RunLoggingProcess(CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                IList<DiagnosticMsg> messages = await _logChannel.Reader.ReadAsync(cancel);
                try
                {
                    Stopwatch watch1 = Stopwatch.StartNew();
                    byte[] data = ProtobufUtil.Compress(messages, 1024);
                    watch1.Stop();

                    Stopwatch watch2 = Stopwatch.StartNew();
                    while (_hubAdapter == null)
                        await Task.Delay(TimeSpan.FromSeconds(1), cancel);

                    Debug.WriteLine($"RegistrationHandler sending {data.Length} bytes");
                    await _hubAdapter.LogEvents(data).ConfigureAwait(false);
                    watch2.Stop();
                    Debug.WriteLine($"RegistrationHandler sent {data.Length} bytes, zip/send took {watch1.ElapsedMilliseconds}ms/{watch2.ElapsedMilliseconds}ms");
                }
                catch (Exception ex) when (!cancel.IsCancellationRequested)
                {
                    Debug.WriteLine($"Failed to log {messages.Count} messages: {ex.Message}");
                }
            }

            Debug.WriteLine($"RunLoggingProcess HAS NOW STOPPED");
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine($"RegistrationHandler.RunLoggingProcess cancelled");
        }
    }

    private async Task RunRegistrationProcess(CancellationToken cancel)
    {
        Stopwatch lastRenew = new Stopwatch();
        
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                while (!cancel.IsCancellationRequested && lastRenew.IsRunning && lastRenew.Elapsed < _renewTime)
                    await Task.Delay(TimeSpan.FromSeconds(5), cancel);

                cancel.ThrowIfCancellationRequested();

                await OpenHub();

                cancel.ThrowIfCancellationRequested();

                _registration.RenewTimeSeconds = (int)_renewTime.TotalSeconds;
                await _hubAdapter.Register(_registration, cancel);
            }
            catch (Exception ex)
            {
                //Something went wrong, so kill the connection and try again
                await CloseConnection();

                if (!cancel.IsCancellationRequested)
                {
                    Trace.WriteLine(ex);
                    string errorMessage = $"DiagnosticHostingService.RegistrationHandler for {UrlInfo} encountered an exception";
                    _log.Warn(errorMessage, ex);
                }
            }
            finally
            {
                lastRenew.Restart();
            }
        }
    }
    
    string UrlInfo => _site.Url == _resolvedUrl ? _site.Url : $"{_site.Url} (resolved to {_resolvedUrl})"; 

    private async Task CloseConnection()
    {
        _hubAdapter = null;
        Debug.WriteLine($"CloseConnection _hubAdapter set to null");

        HubConnection connection = _connection;
        _connection = null;
        try
        {
            if (connection != null)
                await connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
    
    
    
    public class NegotiateResponse
    {
        public string Url { get; set; }
        public string AccessToken { get; set; }
    }


    private async Task OpenHub()
    {
        if (_hubAdapter == null)
        {
            
            
            Debug.WriteLine("Diagnostic RegistrationHandler constructing connection");
            string accessToken = null;
            _resolvedUrl = _site.Url;
            bool isAzure = false;
            IFlurlClient flurlClient = _flurlCache.GetOrAdd("Default");

            if (_site.Url.EndsWith("negotiate", StringComparison.InvariantCultureIgnoreCase))
            {
                isAzure = true;
                string baseUrl = Regex.Replace(_site.Url, "/negotiate", "", RegexOptions.IgnoreCase);
                flurlClient = _flurlCache.GetOrAdd($"Diagnostics_{baseUrl}", baseUrl,
                    options => options.Settings.JsonSerializer = new DefaultJsonSerializer(DiagJsonOptions.Options));

                var negResponse = await flurlClient
                    .Request("negotiate")
                    .GetAsync()
                    .ReceiveJson<NegotiateResponse>();
                
                _resolvedUrl = negResponse.Url;
                accessToken = negResponse.AccessToken;
            }
            
            _connection = new HubConnectionBuilder()
                .AddJsonProtocol(options => options.PayloadSerializerOptions = DiagJsonOptions.Options)
                .WithUrl(_resolvedUrl, options =>
                {
                    options.UseDefaultCredentials = !isAzure;
                    options.AccessTokenProvider = () => Task.FromResult(accessToken);
                })
                .Build();

            _connection.Closed += HandleClosed;

            Debug.WriteLine("Diagnostic RegistrationHandler starting connection");
            await _connection.StartAsync(_stopToken.Token);

            Debug.WriteLine("Diagnostic RegistrationHandler connection started");
            _hubAdapter = new HubServerAdapter(_connection, flurlClient, isAzure);
            _hubAdapter.RenewTimeChanged += (sender, args) => _renewTime = args.Time;
        }
    }

    private async Task HandleClosed(Exception ex)
    {
        Debug.WriteLine($"RegistrationHandler.HandleClosed {ex?.Message}");
        HubConnection currentConnection = _connection;
        HubServerAdapter currentAdapter = _hubAdapter;

        _connection = null;
        _hubAdapter = null;

        try
        {
            currentAdapter?.Dispose();
        }
        catch (Exception ex2)
        {
            Trace.WriteLine("RegistrationHandler.HandleClosed HubServerAdapter.Dispose: " + ex2);
        }

        try
        {
            if (currentConnection != null)
                await currentConnection.DisposeAsync();
        }
        catch (Exception ex2)
        {
            Trace.WriteLine("RegistrationHandler.HandleClosed HubConnection.DisposeAsync: " + ex2);
        }
    }

    public async Task Stop()
    {
        try
        {
            await Deregister(_hubAdapter, _registration);
            
            Task loopTask = _registrationLoop;
            _stopToken?.Cancel();
            
            _hubAdapter?.Dispose();
            _hubAdapter = null;

            _logSubject?.OnCompleted();
            _logSubject = null;

            _logChannel?.Writer.Complete();
            _logChannel = null;

            _registrationLoop = null;
            _stopToken = null;

            if (loopTask != null)
                await loopTask.ConfigureAwait(false);

            if (_connection != null)
                await _connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }

    private async Task Deregister(HubServerAdapter hubAdapter, Registration registration)
    {
        try
        {
            if (hubAdapter != null)
            {
                _log.Info("DiagnosticHostingService Deregistered");
                await hubAdapter.Deregister(registration);
                Debug.WriteLine("Deregistered successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to deregister {ex}");
            _log.Error(ex);
        }
    }

    public void LogEvent(DiagnosticMsg evt)
    {
        _logSubject.OnNext(evt);
    }
}