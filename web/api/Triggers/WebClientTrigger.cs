using DiagnosticExplorer.Api.Triggers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimerInfo = Microsoft.Azure.Functions.Worker.TimerInfo;

namespace api.Triggers;

public class WebClientTrigger : TriggerBase
{

    public WebClientTrigger(ILogger<AccountTrigger> logger, CosmosClient client) : base(logger, client)
    {

        
    }

    [Function("negotiate")]
    public static IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
        [SignalRConnectionInfoInput(HubName = "diagnostics", ConnectionStringSetting = "AzureSignalRConnectionString")] SignalRConnectionInfo connectionInfo)
    {
        Console.WriteLine($"ConnectionInfo is null: {connectionInfo == null} {connectionInfo?.Url} {connectionInfo?.AccessToken}");
        return new OkObjectResult(connectionInfo);
    }


    [Function("Broadcast")]
    [SignalROutput(HubName = "diagnostics")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Broadcast")]
        SignalRInvocationContext invocationContext,
        string data, // message payload
        ILogger logger)
    {
        logger.LogInformation($"Received message from client: {data}");

        // Broadcast the received message to all clients
        return new OkObjectResult(new SignalRMessageAction(
            target: "newMessage",
            arguments: [$"Message sent at {DateTime.Now}: {data}"]
        ));
    }

    
    
    // [Function("broadcast")]
    // [SignalROutput(HubName = "diagnostics")]
    // public static async Task<SignalRMessageAction> Broadcast(
        // [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
    // {
        // return new SignalRMessageAction("message", [$"Message at {DateTime.Now:F}"]);
    // }


}
