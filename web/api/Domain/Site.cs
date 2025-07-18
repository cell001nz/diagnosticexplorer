

namespace DiagnosticExplorer.Api.Domain;

public class Site
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public List<SiteRole> Roles { get; set; } = [];
}

