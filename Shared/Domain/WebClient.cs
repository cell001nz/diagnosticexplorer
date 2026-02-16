namespace DiagnosticExplorer.Domain;

public class WebClient
{
    public int Id { get; set; }
    public string ConnectionId { get; set; } = "";
    public int AccountId { get; set; }
    
    //Subscriptions by ProcessId
    public WebProcSub[] Subscriptions { get; set; } = [];
}