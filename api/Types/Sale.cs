namespace api;

public class Sale
{
    public string Id { get; set; }
    public DateTime Date { get; set; }
    public SaleItem[] Items { get; set; } = [];
}