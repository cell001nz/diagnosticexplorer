namespace DiagnosticExplorer.DataAccess.Entities;

public class SiteEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public AccountEntity? Account { get; set; }
    public List<ProcessEntity> Processes { get; set; } = [];
    public List<SecretEntity> Secrets { get; set; } = [];

}

