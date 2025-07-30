namespace DiagnosticExplorer;

public class DiagnosticOptions
{
    public DiagnosticSite[] Sites { get; set; } = [];
}

public class DiagnosticSite
{
    public string Url { get; set; }
    public string Code { get; set; }
    public string Secret { get; set; }
    public bool Enabled { get; set; } = true;
}