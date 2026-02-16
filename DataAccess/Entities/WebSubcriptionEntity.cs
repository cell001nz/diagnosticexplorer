namespace DiagnosticExplorer.DataAccess.Entities;

public class WebSubcriptionEntity
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int ProcessId { get; set; }
    
    public WebSessionEntity? Session { get; set; }
    public ProcessEntity? Process { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime RenewedAt { get; set; }
    
    
}