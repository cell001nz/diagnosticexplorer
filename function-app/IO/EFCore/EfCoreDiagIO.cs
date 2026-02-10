using Microsoft.Extensions.Logging;

namespace DiagnosticExplorer.IO.EFCore;

public class EfCoreDiagIO : IDiagIO
{
    public EfCoreDiagIO(DiagDbContext context, ILogger<EfCoreDiagIO> logger)
    {
        Site = new SiteIO(context, logger);
        Process = new ProcessIO(context, logger);
        Account = new AccountIO(context, logger);
        SinkEvent = new SinkEventIO(context, logger);
        Values = new DiagValueIO(context, logger);
        WebClient = new WebClientIO(context, logger);
    }

    public ISiteIO Site { get; }
    public IProcessIO Process { get; }
    public IAccountIO Account { get; }
    public ISinkEventIO SinkEvent { get; }
    public IDiagValueIO Values { get; }
    public IWebClientIO WebClient { get; }
}

