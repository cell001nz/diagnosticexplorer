namespace DiagnosticExplorer.DataAccess.Entities;

public class WebSessionEntity
{
    public int Id { get; set; }
    public string ConnectionId { get; set; } = "";
    public int AccountId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    public AccountEntity? Account { get; set; }
    //Subscriptions by ProcessId
    public List<WebSubcriptionEntity> Subscriptions { get; set; } = [];
}