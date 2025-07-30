using DiagnosticExplorer.Domain;

namespace DiagnosticExplorer.IO;

public interface IDiagValueIO
{
    Task Save(DiagValues values);
    Task<DiagValues> Get(string processId, string siteId);
}