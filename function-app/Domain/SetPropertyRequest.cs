namespace DiagnosticExplorer.Api.Domain;

public class SetPropertyRequest
{
    public string ProcessId { get; set; }
    
    public string SiteId { get; set; }

    public string Path { get; set; }

    public string? Value { get; set; }
}