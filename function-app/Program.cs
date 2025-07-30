using DiagnosticExplorer.Api.Triggers;
using DiagnosticExplorer.IO;
using DiagnosticExplorer.IO.Cosmos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});



builder.Services
    .AddSingleton<CosmosClient>(services => new CosmosClientBuilder(Environment.GetEnvironmentVariable("CosmosDBConnection"))
        .WithSystemTextJsonSerializerOptions(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        }).Build())
    .AddSingleton<IDiagIO, DiagIO>()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();