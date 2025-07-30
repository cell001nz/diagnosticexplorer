using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace DiagnosticExplorer.IO.Cosmos;

internal class CosmosIOBase
{
    public const string COSMOS_DB = "diagnosticexplorer";
    protected CosmosClient Client { get; }
    protected Container Container { get; }

    public CosmosIOBase(CosmosClient client, string container)
    {
        Client = client;
        Container = Client.GetContainer(COSMOS_DB, container);
    }
    
    #region ReadSingle

    protected static async Task<T> ReadSingle<T>(Container container, QueryDefinition query, Func<string>? descr = null)
    {
        var iterator = container.GetItemQueryIterator<T>(query);
        FeedResponse<T> response = await iterator.ReadNextAsync();
        if (IsFailure(response.StatusCode))
        {
            string actualDescr = descr?.Invoke() ?? typeof(T).Name;
            string msg = $"Error retrieving {actualDescr}: {response.StatusCode}";
            throw new ApplicationException(msg);
        }

        return response.FirstOrDefault()
               ?? throw new ApplicationException($"{descr} not found");
    }
    
    #endregion
    
    #region ReadMulti

    protected static async Task<T[]> ReadMulti<T>(Container container, QueryDefinition query, Func<string>? descr = null)
    {
        var iterator = container.GetItemQueryIterator<T>(query);

        List<T> items = [];
        while (iterator.HasMoreResults)
        {
            var next = await iterator.ReadNextAsync();
            if (IsFailure(next.StatusCode))
            {
                string actualDescr = descr?.Invoke() ?? typeof(T).Name;
                string msg = $"Error retrieving {actualDescr}: {next.StatusCode}";
                throw new ApplicationException(msg);
            }
            items.AddRange(next.Resource);
        }

        return items.ToArray();
    }
    
    #endregion

    protected static bool IsSuccess(HttpStatusCode code) => (int)code is >= 200 and < 300;
    protected static bool IsFailure(HttpStatusCode code) => !IsSuccess(code);
    protected static bool IsSuccess<TResp>(ItemResponse<TResp> response) => IsSuccess(response.StatusCode);
    protected static bool IsFailure<TResp>(ItemResponse<TResp> response) => IsFailure(response.StatusCode);

}