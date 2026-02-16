namespace DiagnosticExplorer.Domain;

public class SetPropertyRequest
{
    public int ProcessId { get; set; }

    public string? Path { get; set; }

    public string? Value { get; set; }
}