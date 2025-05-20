namespace api;

public class Item
{
    public Item()
    {
    }

    public Item(string id, string title, decimal price)
    {
        Id = id;
        Title = title;
        Price = price;
    }

    public string? Id { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
}