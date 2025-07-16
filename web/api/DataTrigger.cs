using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorerApi;

public class DataTrigger
{
    private readonly ILogger<DataTrigger> _logger;

    public DataTrigger(ILogger<DataTrigger> logger)
    {
        _logger = logger;
    }

    [Function("DataTrigger")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        string username = GetAuthenticatedUsername(req);
        string message = string.IsNullOrEmpty(username)
            ? "Welcome to Azure Functions Captain Anon!"
            : $"Welcome to Azure Functions, {username}!";

        return new OkObjectResult(message);
    }


    
    /// <summary>
    /// Extracts the authenticated username from the x-ms-client-principal header.
    /// </summary>
    private string? GetAuthenticatedUsername(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("x-ms-client-principal", out var header))
            return null;

        string? principalEncoded = header.FirstOrDefault();
        if (principalEncoded == null)
            return null;

        var decodedBytes = Convert.FromBase64String(principalEncoded);
        var json = Encoding.UTF8.GetString(decodedBytes);
        return json;

        var principal = JsonSerializer.Deserialize<ClientPrincipal>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return principal?.UserDetails;
    }

// Model for deserializing the client principal from the header
    public class ClientPrincipal
    {
        public string IdentityProvider { get; set; }
        public string UserId { get; set; }
        public string UserDetails { get; set; }
        public IEnumerable<string> UserRoles { get; set; }
    }
    
}
