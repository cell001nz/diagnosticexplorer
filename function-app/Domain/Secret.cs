namespace DiagnosticExplorer.Domain;

public class Secret
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Hash { get; set; } = "";
}
