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
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}