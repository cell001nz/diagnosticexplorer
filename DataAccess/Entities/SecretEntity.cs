namespace DiagnosticExplorer.DataAccess.Entities;

public class SecretEntity
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Hash { get; set; } = "";
    public SiteEntity? Site { get; set; }
}

