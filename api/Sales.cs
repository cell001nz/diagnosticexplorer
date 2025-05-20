using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using static System.String;

namespace api;

public class Sales
{
    private readonly ILogger<Sales> _logger;
    private CosmosClient _cosmosClient = new(
        Environment.GetEnvironmentVariable("CosmosDBConnectionString"), 
        new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
    

    public Sales(ILogger<Sales> logger)
    {
        _logger = logger;
    }

    [Function("Sales")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function,
            "get", "post", "put", "delete", Route = "sales/{id?}")]
        HttpRequest req, string id)
    {
        try
        {
            var database = _cosmosClient.GetDatabase("SWAStore");
            var container = database.GetContainer("Sales");

            if (req.Method == "GET" && !IsNullOrWhiteSpace(id))
                return await FindById(req, container, id);
            
            if (req.Method == "GET")
                return await FindAll(req, container);

            if (req.Method == "DELETE")
                return await Delete(req, container, id);

            if (req.Method == "POST")
                return await Insert(req, container);

            if (req.Method == "PUT")
                return await Update(req, container);

            return new BadRequestObjectResult("Method Not Allowed") { StatusCode = 405 };
        }
        catch (Exception ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = 500 };
        }
    }

    private static async Task<IActionResult> FindById(HttpRequest req, Container container, string id)
    {
        ItemResponse<Sale>? item = await container.ReadItemAsync<Sale>(id, new PartitionKey(id));
        return new ObjectResult(item.Resource);
    }
    
    private static async Task<IActionResult> FindAll(HttpRequest req, Container container)
    {
        List<Sale> sales = [];
        var iter = container.GetItemQueryIterator<Sale>();
        while (iter.HasMoreResults)
        {
            var next = await iter.ReadNextAsync();
            sales.AddRange(next.Resource);
        }

        return new ObjectResult(sales);
    }
    
    private static async Task<IActionResult> Insert(HttpRequest req, Container container)
    {
        Sale sale = (await req.ReadFromJsonAsync<Sale>())!;
        try
        {
            if (IsNullOrWhiteSpace(sale.Id))
                sale.Id = Guid.NewGuid().ToString("N");

            await container.CreateItemAsync(sale);
            return new OkObjectResult($"Sale created.");
        }
        catch (Exception ex)
        {
            return new ObjectResult($"Failed to create Sale with Id {sale.Id}: {ex.Message}") { StatusCode = 500 };
        }
    }

    private static async Task<IActionResult> Update(HttpRequest req, Container container)
    {
        Sale sale = (await req.ReadFromJsonAsync<Sale>())!;
        await container.ReplaceItemAsync(sale, sale.Id);
        return new OkObjectResult($"Sale {sale.Id} updated.");
    }

    private static async Task<IActionResult> Delete(HttpRequest req, Container container, string id)
    {
        ItemResponse<Sale>? item = await container.DeleteItemAsync<Sale>(id, new PartitionKey(id));
        return new OkObjectResult($"Sale {id} deleted.");
    }
}