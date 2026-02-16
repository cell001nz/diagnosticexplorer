using System.Threading.Tasks;
using DiagnosticExplorer.Api.Domain;

namespace DiagnosticExplorer.Domain;



public interface IWebHub
{
    
}

public interface IWebHubClient
{
    Task ReceiveProcess(DiagProcess process);
    Task ClearEvents(int processId);
    Task StreamEvents(int processId, SystemEvent[] events);
    Task ReceiveDiagnostics(int processId, DiagnosticResponse response);
}

public interface IProcessHub
{
    Task Register(Registration registration);
    Task ClearEvents(int processId);
    Task StreamEvents(int processId, SystemEvent[] events);
    Task ReceiveDiagnostics(int processId, string stringData);
}

public interface IProcessHubClient
{
    Task ExecuteOperation(string requestId, string path, string operation, string[] arguments);
    Task SetProperty(string requestId, string path, string value);
    Task SetRenewTime(int millis);
    Task StartSending(int millis);
    Task SetProcessId(int processId);
     Task StopSending();
     Task ReceiveMessage(string message);
}
