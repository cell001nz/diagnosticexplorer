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
using Microsoft.Extensions.Primitives;

namespace DiagnosticExplorer.Api.Triggers;

public class TriggerBase
{
    protected readonly ILogger _logger;
    // protected readonly CosmosClient _cosmosClient;
    public const string CLIENT_HUB = "client";
    public const string MESSAGES = "messages";
    public const string WEB_HUB = "web";
    
    protected IDiagIO DiagIO { get; private set; }

    public TriggerBase(ILogger logger, IDiagIO diagIO)
    {
        _logger = logger;
        DiagIO = diagIO ?? throw new ArgumentNullException(nameof(diagIO));
    }

    /// <summary>
    /// Extracts the authenticated username from the x-ms-client-principal header.
    /// </summary>
    protected ClientPrincipal GetClientPrincipal(SignalRInvocationContext req)
    {
        if (!req.Headers.TryGetValue("x-ms-client-principal", out var header))
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

}
