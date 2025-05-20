using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using static System.String;

namespace api;

public class Items
{
    private readonly ILogger<Items> _logger;
    
    private CosmosClient _cosmosClient = new(
        Environment.GetEnvironmentVariable("CosmosDBConnectionString"), 
        new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });

    public Items(ILogger<Items> logger)
    {
        _logger = logger;
    }

    [Function("Items")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function,
            "get", "post", "put", "delete", Route = "items/{id?}")]
        HttpRequest req, string id)
    {
        try
        {
            var database = _cosmosClient.GetDatabase("SWAStore");
            var container = database.GetContainer("Items");
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
        ItemResponse<Item>? item = await container.ReadItemAsync<Item>(id, new PartitionKey(id));
        return new ObjectResult(item.Resource);
    }

    private static async Task<IActionResult> FindAll(HttpRequest req, Container container)
    {
        List<Item> items = [];
        var iter = container.GetItemQueryIterator<Item>();
        while (iter.HasMoreResults)
        {
            FeedResponse<Item>? next = await iter.ReadNextAsync();
            items.AddRange(next.Resource);
        }

        return new ObjectResult(items);
    }

    private static async Task<IActionResult> Insert(HttpRequest req, Container container)
    {
        Item item = (await req.ReadFromJsonAsync<Item>())!;
        try
        {
            if (IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N");

            await container.CreateItemAsync(item);
            return new OkObjectResult($"Item created.");
        }
        catch (Exception ex)
        {
            return new ObjectResult($"Failed to create Item with Id {item.Id}: {ex.Message}") { StatusCode = 500 };
        }
    }

    private static async Task<IActionResult> Update(HttpRequest req, Container container)
    {
        Item item = (await req.ReadFromJsonAsync<Item>())!;
        await container.ReplaceItemAsync(item, item.Id);
        return new OkObjectResult($"Item {item.Id} updated.");
    }

    private static async Task<IActionResult> Delete(HttpRequest req, Container container, string id)
    {
        ItemResponse<Item>? item = await container.DeleteItemAsync<Item>(id, new PartitionKey(id));
        return new OkObjectResult($"Item {id} deleted.");
    }
}