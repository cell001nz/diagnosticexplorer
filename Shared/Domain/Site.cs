namespace DiagnosticExplorer.Domain;

public class Site
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public List<Secret>? Secrets { get; set; } = [];
}
