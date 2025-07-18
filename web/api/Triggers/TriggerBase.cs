using System.Collections;
using System.Text;
using System.Text.Json;
using api.Triggers;
using DiagnosticExplorer.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.Api;

public class TriggerBase
{
    protected readonly ILogger _logger;
    protected readonly CosmosClient _cosmosClient;
    protected const string DIAGNOSTIC_EXPLORER = "diagnosticexplorer";


    public TriggerBase(ILogger logger, CosmosClient client)
    {
        _logger = logger;
        _cosmosClient = client ?? throw new ArgumentNullException(nameof(client));

    }


    /// <summary>
    /// Extracts the authenticated username from the x-ms-client-principal header.
    /// </summary>
    protected ClientPrincipal GetClientPrincipal(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("x-ms-client-principal", out var header))
            return null;

        string? principalEncoded = header.FirstOrDefault();
        if (principalEncoded == null)
            return null;

        var decodedBytes = Convert.FromBase64String(principalEncoded);
        var json = Encoding.UTF8.GetString(decodedBytes);

        return JsonSerializer.Deserialize<ClientPrincipal>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

}
