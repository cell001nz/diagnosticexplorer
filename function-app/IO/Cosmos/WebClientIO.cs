using DiagnosticExplorer.Api.Domain;
using DiagnosticExplorer.IO.Cosmos;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using WebClient = DiagnosticExplorer.Api.Domain.WebClient;

namespace DiagnosticExplorer.IO.Cosmos;

internal class WebClientIO : CosmosIOBase, IWebClientIO
{
    
    public WebClientIO(CosmosClient client) : base(client, "WebClient")
    {
    }
   
    #region Save(WebClient client)

    public async Task<WebClient> Save(WebClient client)
    {
        var response = await Container.UpsertItemAsync(client, new PartitionKey(client.Id));
        return response.Resource;
    }
    
    #endregion
    
    #region Delete(string clientId)

    public async Task Delete(string clientId)
    {
        await Container.DeleteItemAsync<WebClient>(clientId, new PartitionKey(clientId));
    }
    
    #endregion
}

