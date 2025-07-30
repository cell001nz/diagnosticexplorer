namespace DiagnosticExplorer.IO;

public interface ISinkEventIO
{
    Task DeleteForProcess(string processId);
    Task Save(SystemEvent[] events);
}