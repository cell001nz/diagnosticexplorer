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
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace DiagnosticExplorer.IO.Cosmos;

internal class SinkEventIO(CosmosClient client) : CosmosIOBase(client, "SinkEvent"), ISinkEventIO
{
 
    #region DeleteForProcess(string processId)
    
    public async Task DeleteForProcess(string processId)
    {
        await Container.DeleteAllItemsByPartitionKeyStreamAsync(new PartitionKey(processId));
    }

    #endregion
    
    #region Save(SystemEvent[] events)
    
    public async Task Save(SystemEvent[] events)
    {
        TransactionalBatch batch = Container.CreateTransactionalBatch(new PartitionKey(events[0].ProcessId));
        foreach (var evt in events)
        {
            evt.Id = Guid.NewGuid().ToString("N");
            batch.CreateItem(evt);
        }

        await batch.ExecuteAsync();
    }

    #endregion
  
}

