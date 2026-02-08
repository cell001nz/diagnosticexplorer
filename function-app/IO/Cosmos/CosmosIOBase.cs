using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.Cosmos;

internal class CosmosIOBase<T> where T : class
{
    public const string COSMOS_DB = "diagnosticexplorer";
    protected CosmosClient Client { get; }
    protected Container Container { get; }
    protected ILogger Logger { get; }

    public CosmosIOBase(CosmosClient client, string container, ILogger logger)
    {
        Client = client;
        Container = Client.GetContainer(COSMOS_DB, container);
        Logger = logger;
    }

    #region GetItem

    protected async Task<T?> GetItem(string id, string? partitionKey = null)
    {
        try
        {
            var response = await Container.ReadItemAsync<T>(id, new PartitionKey(partitionKey ?? id));
            Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} GetItem<{typeof(T).Name}> ({id}/{partitionKey})");

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    #endregion
    
    #region ReadSingle

    protected async Task<T?> ReadSingle(QueryDefinition query, Func<string>? descr = null)
    {
        var iterator = Container.GetItemQueryIterator<T>(query);
        FeedResponse<T> response = await iterator.ReadNextAsync();
        
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} ReadSingle<{typeof(T).Name}> ({descr?.Invoke()})");
        try
        {
            return response.FirstOrDefault();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    #endregion
    
    #region ReadMulti

    protected async Task<T[]> ReadMulti(QueryDefinition query, Func<string>? descr = null)
    {
        var iterator = Container.GetItemQueryIterator<T>(query);

        double charge = 0;
        List<T> items = [];
        while (iterator.HasMoreResults)
        {
            var next = await iterator.ReadNextAsync();
            charge += next.RequestCharge;
            items.AddRange(next.Resource);
        }

        Trace.WriteLine($"*** CHARGE *** {charge} ReadMulti<{typeof(T).Name}> ({descr?.Invoke()})");

        return items.ToArray();
    }
    
    #endregion
    
    protected void ReportCharge(ItemResponse<T> response, string message)
    {
        Trace.WriteLine($"*** CHARGE *** {response.RequestCharge} ({message})");
    }


    protected static bool IsSuccess(HttpStatusCode code) => (int)code is >= 200 and < 300;
    protected static bool IsFailure(HttpStatusCode code) => !IsSuccess(code);
    protected static bool IsSuccess<TResp>(ItemResponse<TResp> response) => IsSuccess(response.StatusCode);
    protected static bool IsFailure<TResp>(ItemResponse<TResp> response) => IsFailure(response.StatusCode);

}