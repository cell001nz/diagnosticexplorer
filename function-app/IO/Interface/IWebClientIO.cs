using DiagnosticExplorer.Api.Domain;

namespace DiagnosticExplorer.IO;

public interface IWebClientIO
{
    Task<WebClient> Save(WebClient client);
    Task<WebClient?> Get(string connectionId);
    Task Delete(string clientId);
    Task SaveWebSub(WebProcSub sub);
    Task DeleteWebSub(WebProcSub sub);
}