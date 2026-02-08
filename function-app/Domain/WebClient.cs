namespace DiagnosticExplorer.Api.Domain;

public class WebClient
{
    public string Id { get; set; }
    public string AccountId { get; set; }
    
    
    //Subscriptions by ProcessId
    public Dictionary<string, WebProcSub> Subscriptions { get; set; } = [];
}