namespace DiagnosticExplorer.DataAccess.Entities;

public class ProcessEntity
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string? InstanceId { get; set; }
    public string? ConnectionId { get; set; }
    public bool IsOnline { get; set; }
    public string Name { get; set; } = "";
    public string? UserName { get; set; }
    public string? MachineName { get; set; }
    public bool IsSending { get; set; }
    public SiteEntity? Site { get; set; }
    public List<WebSubcriptionEntity> Subscriptions { get; set; } = [];
    public DateTime LastReceived { get; set; }
    public DateTime LastOnline { get; set; }
}

