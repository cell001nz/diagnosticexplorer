namespace DiagnosticExplorer.Api.Domain;

public class SiteRole
{
    public string AccountId { get; set; } = "";
    public SiteRoleType Role { get; set; } = SiteRoleType.View;
}
