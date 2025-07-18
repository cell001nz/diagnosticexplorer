namespace DiagnosticExplorer.Api.Domain;

public class Account
{
    public Account()
    {
    }

    public Account(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; set; }
    public string Name { get; set; } = "";
}
