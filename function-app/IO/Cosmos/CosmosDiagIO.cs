using DiagnosticExplorer.Api.Domain;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.Cosmos;

public class CosmosDiagIO : IDiagIO
{

    public CosmosDiagIO(CosmosClient client, ILogger<CosmosDiagIO> logger)
    {
        Trace.WriteLine($"****************************************************************************************** Got logger {logger}");
        
        Site = new SiteIO(client, logger);
        Process = new ProcessIO(client, logger);
        Account = new AccountIO(client, logger);
        SinkEvent = new SinkEventIO(client, logger);
        Values = new DiagValueIO(client, logger);
        WebClient = new WebClientIO(client, logger);
    }

    public ISiteIO Site { get; }
    public IProcessIO Process { get; }
    public IAccountIO Account { get; }
    public ISinkEventIO SinkEvent { get; }
    public IDiagValueIO Values { get; }
    public IWebClientIO WebClient { get; }
}

