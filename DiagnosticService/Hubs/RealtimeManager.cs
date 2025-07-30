﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiagnosticExplorer;
using DiagnosticExplorer.Common;
using Diagnostics.Service.Common.Transport;
using DiagWebService.ClientHandlers;
using log4net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Diagnostics.Service.Common.Hubs;

public class RealtimeManager : IHostedService
{
    private static readonly StringComparer _ic = StringComparer.InvariantCultureIgnoreCase;
    private readonly ReaderWriterLockSlim _configLockObj = new(LockRecursionPolicy.SupportsRecursion);
    private readonly ILog _log = LogManager.GetLogger(typeof(RealtimeManager));
    private readonly ConcurrentDictionary<string, DiagProcess> _processes = new();
    private readonly ConcurrentDictionary<string, WebClientHandler> _webClients = new();
    public EventSink RealtimEvents { get; } = EventSinkRepo.Default.GetSink("Realtime Events", "Realtime");


    private readonly ConcurrentDictionary<DiagProcess, DiagnosticSubscription> _subscriptions = new();
    public Subject<DiagProcess> ProcessChanged { get; } = new();
    public Subject<DiagProcess> ProcessRemoved { get; } = new();
    private IDisposable? _alertLevelSubscription;

    private static readonly TimeSpan _alertDuration = TimeSpan.FromSeconds(2);

   [CollectionProperty(CollectionMode.Categories, Category = "Processes", CategoryProperty = nameof(DiagProcess.Id))]
    public ICollection<DiagProcess> Processes => _processes.Values;


    [CollectionProperty(CollectionMode.Categories, Category = "Subscriptions", CategoryProperty = nameof(DiagnosticSubscription.ProcessId))]
    public ICollection<DiagnosticSubscription> Subscriptions => _subscriptions.Values;


    public RealtimeManager(IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStarted.Register(() => StartAsync(lifetime.ApplicationStopping));
        lifetime.ApplicationStopping.Register(() => StopAsync(CancellationToken.None));
    }

    public Task StartAsync(CancellationToken cancel)
    {
        _alertLevelSubscription = Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            .Subscribe(_ => ProcessesAlertLevels());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _alertLevelSubscription?.Dispose();
        return Task.CompletedTask;
    }

    public void ProcessesAlertLevels()
    {
        try
        {
            foreach (DiagProcess process in Processes)
            {
                TimeSpan age = DateTime.UtcNow.Subtract(process.AlertLevelDate ?? DateTime.UtcNow);

                if (process.AlertLevel > 0 && age > _alertDuration)
                {
                    process.AlertLevel = 0;
                    process.AlertLevelDate = null;
                    ProcessChanged.OnNext(process);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }
    }

    public void RegisterAlertLevel(string connectionId, DiagnosticMsg[] messages)
    {
        int level = messages.Where(m => DateTime.UtcNow.Subtract(m.Date) < _alertDuration)
            .Select(m => m.Level)
            .DefaultIfEmpty(0)
            .Max();

        DiagProcess? process= Processes.FindByConnectionId(connectionId);
        if (process != null && process.AlertLevel < level)
        {
            process.AlertLevel = level;
            process.AlertLevelDate = DateTime.UtcNow;
            ProcessChanged.OnNext(process);
        }
    }

    
        
    public ICollection<DiagProcess> GetProcesses()
    {
        return Processes;
    }

  
    private readonly ConcurrentDictionary<string, DiagnosticClientHandler> _diagClients = new();


    [RateProperty(Category = "Requests", ExposeTotal = true, ExposeRate = true)]
    public RateCounter ConfigRequests { get; set; } = new(3);

    [RateProperty(Category = "Requests", ExposeTotal = true, ExposeRate = true)]
    public RateCounter DiagnosticRequests { get; set; } = new(3);

    [RateProperty(Category = "Requests", ExposeTotal = true, ExposeRate = true)]
    public RateCounter Registrations { get; set; } = new(3);

    [RateProperty(Category = "Requests", ExposeTotal = true, ExposeRate = true)]
    public RateCounter Deregistrations { get; set; } = new(3);

    [Property(Category = "Processes")] public int TotalProcesses => _processes.Count;

    internal void AddDiagnosticClient(DiagnosticClientHandler client)
    {
        _diagClients.TryAdd(client.ConnectionId, client);
        RealtimEvents.Notice($"Client {client.ConnectionId} added");

        client.Disconnected += HandleClientDisconnected;
    }

    private void HandleClientDisconnected(object? sender, EventArgs e)
    {
        DiagnosticClientHandler client = (DiagnosticClientHandler) sender;
        RealtimEvents.Notice($"Client {client.ConnectionId} disconnected");
        Deregister(client);
    }

    internal DiagnosticClientHandler? GetClientHandler(string connectionId)
    {
        _diagClients.TryGetValue(connectionId, out DiagnosticClientHandler? client);
        return client;
    }

    private void EnterConfigLock()
    {
        if (!_configLockObj.TryEnterWriteLock(TimeSpan.FromSeconds(1000)))
            throw new ApplicationException("Failed to obtain config write lock");
    }

    private void ExitConfigLock()
    {
        _configLockObj.ExitWriteLock();
    }

    public void RemoveProcess(string id)
    {
        EnterConfigLock();
        try
        {
            _processes.TryRemove(id, out DiagProcess? item);

            if (item == null)
                throw new ApplicationException($"Can't find item '{id}'");

            if (item.ConnectionId != null)
                GetClientHandler(item.ConnectionId)?.CloseConnection();

            ProcessRemoved.OnNext(item);
        }
        finally
        {
            ExitConfigLock();
        }
    }

    public async Task<OperationResponse> SetProperty(SetPropertyRequest request)
    {
        try
        {
            DiagProcess? p = GetProcess(request.Id);
            if (p == null)
                return OperationResponse.Error($"Process {request.Id} not found");

            IDiagnosticClient? client = GetSubscription(p)?.DiagnosticClient;
            if (client == null)
                return OperationResponse.Error($"Process {request.Id} is not connected");

            return await client.SetProperty(request.Path, request.Value);
        }
        catch (Exception ex)
        {
            return OperationResponse.Error(ex.Message);
        }
    }

    public async Task<OperationResponse> ExecuteOperation(ExecuteOperationRequest request)
    {
        try
        {
            DiagProcess? p = GetProcess(request.Id);
            if (p == null)
                return OperationResponse.Error($"Process {request.Id} not found");

            IDiagnosticClient? client = GetSubscription(p)?.DiagnosticClient;
            if (client == null)
                return OperationResponse.Error($"Process {request.Id} is not connected");

            return await client.ExecuteOperation(request.Path, request.Operation, request.Arguments);
        }
        catch (Exception ex)
        {
            return OperationResponse.Error(ex.Message);
        }
    }

    private static void SetStatus(DiagProcess group, OnlineState state, string? message)
    {
        group.State = state;
        group.Message = message;
        if (group.State == OnlineState.Online)
            group.LastOnline = DateTime.UtcNow;
    }

    public void Register(Registration registration, string connectionId)
    {
        Registrations.Register(1);
        EnterConfigLock();
        try
        {
            DiagProcess? process = null;
            if (!string.IsNullOrWhiteSpace(registration.InstanceId))
                process = Processes.FindByInstanceId(registration.InstanceId);

            if (process == null)
            {
                DiagProcess[] found = Processes.Where(x =>
                        _ic.Equals(x.MachineName, registration.MachineName)
                        && _ic.Equals(x.ProcessName, registration.ProcessName)
                        && x.ConnectionId == null
                        && (string.IsNullOrEmpty(x.UserName) ||
                            _ic.Equals(x.UserName, registration.UserName)))
                    .ToArray();

                if (found.Length >= 1)
                    process = found.FirstOrDefault(x => x.State == OnlineState.Offline);
            }

            OnlineState? previousState = process?.State;

            if (process == null)
            {
                process = new DiagProcess();
                process.Id = Guid.NewGuid().ToString("N");
                process.MachineName = registration.MachineName;
                process.ProcessName = registration.ProcessName;
                _processes.TryAdd(process.Id, process);
            }


            process.UserName = registration.UserName;
            process.ProcessId = registration.ProcessId;
            process.State = OnlineState.Online;
            process.LastOnline = DateTime.UtcNow;
            process.ConnectionId = connectionId;
            process.InstanceId = registration.InstanceId;

            SetStatus(process, OnlineState.Online, null);

            if (connectionId != null && _diagClients.TryGetValue(connectionId, out DiagnosticClientHandler? diagClient))
                GetSubscription(process).SetDiagnosticClient(diagClient);

            if (process.State != previousState)
                ProcessChanged.OnNext(process);
        }
        catch (Exception ex)
        {
            _log.Error(registration, ex);
            throw;
        }
        finally
        {
            ExitConfigLock();
        }
    }


    /// <summary>
    ///     Remove any entries which are no longer needed
    /// </summary>
    private void TidyProcesses()
    {
        //Mark as offline anything which is 5 seconds late for renewal
        TimeSpan expiryTime = TimeSpan.FromSeconds(30);

        DiagProcess[] autoOnline = Processes
            .Where(x => x.State == OnlineState.Online)
            .ToArray();

        foreach (DiagProcess proc in autoOnline)
        {
            if (DateTime.UtcNow - proc.LastOnline > expiryTime)
            {
                proc.State = OnlineState.Offline;
                proc.Message = "Failed to renew";
                ProcessChanged.OnNext(proc);
            }
        }

        //Group all items by process, instance and host
        DiagProcess[][] procs = (from x in Processes
            group x by new {x.ProcessName, Host = x.MachineName?.ToLower()}
            into grp
            select grp.ToArray()).ToArray();

        //For each group, remove any excess entries which are offline
        foreach (DiagProcess[] matching in procs)
        {
            //Find the items which are no longer online
            DiagProcess[] toRemove = matching
                .Where(x => x.State != OnlineState.Online)
                .ToArray();

            //If all must be removed, make sure we leave just one
            if (toRemove.Length == matching.Length)
                toRemove = toRemove.Skip(1).ToArray();

            foreach (DiagProcess proc in toRemove)
            {
                _processes.TryRemove(proc.Id, out _);
                ProcessRemoved.OnNext(proc);
            }
        }

        DiagProcess[] expired = Processes.Where(HasExpired).ToArray();
        foreach (DiagProcess proc in expired)
            _processes.TryRemove(proc.Id, out _);
    }


    private bool HasExpired(DiagProcess process)
    {
        if (process.State == OnlineState.Online)
            return false;

        TimeSpan? elapsed = process.LastOnline.HasValue ? DateTime.UtcNow - process.LastOnline : null;

        return elapsed > TimeSpan.FromDays(100);
    }

    public DiagProcess? GetProcess(string id)
    {
        return _processes.TryGetValue(id, out var value) ? value : null;
    }

   
    public void Deregister(Registration registration)
    {
        Deregister(() => Processes.FindByInstanceId(registration.InstanceId));
    }

    private void Deregister(DiagnosticClientHandler client)
    {
        Deregister(() => Processes.FindByConnectionId(client.ConnectionId));
    }

    private void Deregister(Func<DiagProcess?> getProcess)
    {
        Deregistrations.Register(1);

        EnterConfigLock();
        try
        {
            DiagProcess? process = getProcess();
            if (process != null)
            {
                if (process.ConnectionId != null)
                    _diagClients.TryRemove(process.ConnectionId, out _);

                process.State = OnlineState.Offline;
                process.ConnectionId = null;
                process.Message = "Offline";

                if (_subscriptions.TryGetValue(process, out DiagnosticSubscription? subscription))
                    subscription.SetDiagnosticClient(null);

                ProcessChanged.OnNext(process);
            }

            TidyProcesses();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            throw;
        }
        finally
        {
            ExitConfigLock();
        }
    }

    public void AddWebHubClient(string connectionId, IWebHubClient client)
    {
        var handler = new WebClientHandler(connectionId, client);
        _webClients.TryAdd(connectionId, handler);
        handler.Start(this);
    }

    public void RemoveWebHubClient(string connectionId)
    {
        if (_webClients.TryRemove(connectionId, out WebClientHandler? client))
        {
            RemoveClientFromSubscriptions(client);
            client?.Stop();
        }
    }

    private void RemoveClientFromSubscriptions(WebClientHandler client)
    {
        foreach (DiagnosticSubscription sub in _subscriptions.Values)
            sub.RemoveWebClient(client);
    }


    public async Task<bool> SubscribeWebClient(string webConnectionId, string processId)
    {
        if (!_webClients.TryGetValue(webConnectionId, out WebClientHandler? webClient))
            return false;

        RemoveClientFromSubscriptions(webClient);

        if (!_processes.TryGetValue(processId, out DiagProcess? process))
            return false;

        var subscription = _subscriptions.GetOrAdd(process, key => GetSubscription(process));
        await subscription.AddWebClient(webClient);
        return true;
    }

    private DiagnosticSubscription GetSubscription(DiagProcess process)
    {
        return _subscriptions.GetOrAdd(process, key => new(process));
    }

   
}