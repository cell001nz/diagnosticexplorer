namespace DiagnosticExplorer.Domain;

public class WebProcSub
{
    public int ProcessId { get; set; }
    public string WebConnectionId { get; set; } = "";
    public DateTime Date { get; set; }
}