namespace DiagnosticExplorer.IO;

public interface IDiagIO
{
    ISiteIO Site { get; }
    IProcessIO Process { get; }
    IAccountIO Account { get; }
    ISinkEventIO SinkEvent { get; }
    IDiagValueIO Values { get; }
    IWebClientIO WebClient { get; }
}