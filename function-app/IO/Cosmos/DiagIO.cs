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
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace DiagnosticExplorer.IO.Cosmos;

public class DiagIO(CosmosClient client) : IDiagIO
{
    public ISiteIO Site { get; } = new SiteIO(client);
    public IProcessIO Process { get; } = new ProcessIO(client);
    public IAccountIO Account { get; } = new AccountIO(client);
    public ISinkEventIO SinkEvent { get; } = new SinkEventIO(client);
    public IDiagValueIO Values { get; } = new DiagValueIO(client);
    public IWebClientIO WebClient { get; } = new WebClientIO(client);
}

