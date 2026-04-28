namespace InterfaceValidation.Domains;

public sealed class Product
{
    public string Id { get; }
    public string Name { get; }
    private Product(string id, string name) { Id = id; Name = name; }
    public static Product Create(string id, string name) => new(id, name);
}
