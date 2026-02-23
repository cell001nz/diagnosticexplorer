namespace DiagnosticExplorer.Domain;

public class Account
{
    public Account()
    {
    }

    public Account(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public bool IsProfileComplete { get; set; }
}
