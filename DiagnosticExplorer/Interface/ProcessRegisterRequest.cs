namespace DiagnosticExplorer;

public class ProcessRegisterRequest
{
    public string ConnectionId { get; set; }
    public int SiteId { get; set; }
    public Registration Registration { get; set; }
}


