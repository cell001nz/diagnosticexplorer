using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using DiagnosticExplorer.DataAccess;


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

var connectionString = Environment.GetEnvironmentVariable("PostgreSQLConnection") 
    ?? throw new InvalidOperationException("PostgreSQLConnection not configured");

builder.Services
//    .AddSingleton<CosmosClient>(services => new CosmosClientBuilder(Environment.GetEnvironmentVariable("CosmosDBConnection"))
//    .WithSystemTextJsonSerializerOptions(jsonOptions).Build())
    .AddDbContext<DiagDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    })
    .AddHttpContextAccessor()
    // .AddScoped<IAccountIO, AccountIO>()
    // .AddScoped<IProcessIO, ProcessIO>()
    // .AddScoped<ISinkEventIO, SinkEventIO>()
    // .AddScoped<ISiteIO, SiteIO>()
    // .AddScoped<IWebClientIO, WebClientIO>()
    // .AddScoped<IDiagValueIO, DiagValueIO>()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
