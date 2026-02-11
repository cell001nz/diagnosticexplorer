using DiagnosticExplorer.Api.Triggers;
using DiagnosticExplorer.IO;
using DiagnosticExplorer.IO.EFCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using DiagnosticExplorer.IO.Cosmos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;


JsonSerializerOptions jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<WorkerOptions>(workerOptions =>
{
    workerOptions.Serializer = new JsonObjectSerializer(jsonOptions);
});


builder.Services.AddControllers().AddJsonOptions(options =>
{
    // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnection") 
    ?? throw new InvalidOperationException("CosmosDBConnection not configured");

builder.Services
    .AddSingleton<CosmosClient>(services => new CosmosClientBuilder(Environment.GetEnvironmentVariable("CosmosDBConnection"))
    .WithSystemTextJsonSerializerOptions(jsonOptions).Build())
    .AddDbContext<DiagDbContext>(options =>
    {
        options.UseCosmos(
            cosmosConnectionString,
            DiagDbContext.DATABASE_NAME,
            cosmosOptions =>
            {
                cosmosOptions.ConnectionMode(ConnectionMode.Direct);
            });
    })
    // .AddScoped<IDiagIO, CosmosDiagIO>()
    .AddScoped<IDiagIO, CosmosDiagIO>()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
