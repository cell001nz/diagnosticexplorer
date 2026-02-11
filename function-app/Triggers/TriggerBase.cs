using System.Net;
using DiagnosticExplorer.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using DiagnosticExplorer.IO;
using DiagnosticExplorer.Util;
using Microsoft.Extensions.Primitives;

namespace DiagnosticExplorer.Api.Triggers;

public class TriggerBase
{
    protected readonly ILogger _logger;
    protected const string PROCESS_HUB = "process";
    protected const string WEB_HUB = "web";
    protected const string MESSAGES = "messages";
    
    protected const int PROCESS_RENEW_TIME = 20;
    protected const int PROCESS_STALE_TIME = 60;
    protected const int DIAG_SEND_FREQ = 5_000;

    
    protected IDiagIO DiagIO { get; }

    public TriggerBase(ILogger logger, IDiagIO diagIO)
    {
        _logger = logger;
        DiagIO = diagIO ?? throw new ArgumentNullException(nameof(diagIO));
    }

    /// <summary>
    /// Extracts the authenticated username from the x-ms-client-principal header.
    /// </summary>
    protected ClientPrincipal GetClientPrincipal(SignalRInvocationContext invocationContext)
    {
        if (!invocationContext.Headers.TryGetValue("x-ms-client-principal", out var header))
            throw new ApplicationException($"x-ms-client-principal header not found");

        return GetClientPrincipal(header);
    }
    
    protected ClientPrincipal GetClientPrincipal(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("x-ms-client-principal", out var header))
            throw new ApplicationException($"x-ms-client-principal header not found");

        return GetClientPrincipal(header);
    }
    
    protected ClientPrincipal GetClientPrincipal(StringValues header)
    {
        string? principalEncoded = header.FirstOrDefault();
        if (principalEncoded == null)
            throw new ApplicationException($"x-ms-client-principal header not found");

        var decodedBytes = Convert.FromBase64String(principalEncoded);
        var json = Encoding.UTF8.GetString(decodedBytes);

        return JsonSerializer.Deserialize<ClientPrincipal>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    
    protected async Task VerifySiteAccess(ClientPrincipal cp, string siteId)
    {
        await DiagIO.Site.GetSiteForUser(siteId, cp.UserId);
    }


    protected T DeserialiseBase64Protobuf<T>(string strData)
    {
        byte[] data = Convert.FromBase64String(strData);
        return ProtobufUtil.Decompress<T>(data);
    }
 
    
    public class DualHubOutput
    {
        [SignalROutput(HubName = WEB_HUB)]
        public List<object> WebClient { get; } = [];
        
        [SignalROutput(HubName = PROCESS_HUB)]
        public List<object> ProcessClient { get; } = [];
    }
    
}
