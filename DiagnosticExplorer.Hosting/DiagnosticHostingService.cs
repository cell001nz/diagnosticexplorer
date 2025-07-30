using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticExplorer.Log4Net;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;


namespace DiagnosticExplorer;

public class DiagnosticHostingService
#if NET5_0_OR_GREATER
    : IHostedService
#endif
{
    private static DiagnosticHostingService _instance;
    private DiagnosticOptions _options;
    private RegistrationHandler[] _registrationHandlers;
    private IFlurlClientCache _flurlCache;

    private DiagnosticHostingService(DiagnosticOptions options, IFlurlClientCache flurlCache)
    {
        _options = options;
        _flurlCache = flurlCache ?? throw new ArgumentNullException(nameof(flurlCache));
    }

    #if true || NET5_0_OR_GREATER
    
    public DiagnosticHostingService(IOptions<DiagnosticOptions> options, IFlurlClientCache flurlCache) : this(options.Value, flurlCache)
    {
        // Debug.WriteLine($"DiagnosticHostingService constructed {_options.Enabled} Uri [{_options.Uri}");
    }


    public async Task StartAsync(CancellationToken cancel)
    {
        // Debug.WriteLine($"DiagnosticHostingService starting {_options.Enabled} Uri [{_options.Uri}");
        if (_options.Sites.Any(site => site.Enabled))
        {
            _instance = this;
            StartHosting();
        }
    }

    public Task StopAsync(CancellationToken cancel)
    {
        return StopHosting();
    }

#endif


    private void StartHosting()
    {
        try
        {
            DiagnosticRetroAppender.SetLoggingAction(LogEvent);
            SystemStatus.Register();

            _registrationHandlers = _options.Sites
                .Where(site => site.Enabled)
                .Select(site => new RegistrationHandler(site, _flurlCache))
                .ToArray();

            foreach (RegistrationHandler handler in _registrationHandlers)
                handler.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public async Task StopHosting()
    {
        try
        {
            DiagnosticRetroAppender.SetLoggingAction(null);
            await Task.WhenAll(_registrationHandlers.Select(handler => handler.Stop()).ToArray());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        _registrationHandlers = null;
    }


    public static void Start(DiagnosticOptions options, IFlurlClientCache flurlCache)
    {
        if (_instance == null)
        {
            _instance = new DiagnosticHostingService(options, flurlCache);
            _instance.StartHosting();

        }
    }

    public static async Task Stop()
    {
        if (_instance != null)
        {

            await _instance.StopHosting();
            _instance = null;
        }
    }


    public static void LogEvent(DiagnosticMsg evt)
    {
        DiagnosticHostingService instance = _instance;
        if (instance != null)
            // Debug.WriteLine($"Sending to {instance._registrationHandlers?.Length} registration handlers");
            foreach (RegistrationHandler handler in instance._registrationHandlers ?? Array.Empty<RegistrationHandler>())
                handler.LogEvent(evt);
    }
}