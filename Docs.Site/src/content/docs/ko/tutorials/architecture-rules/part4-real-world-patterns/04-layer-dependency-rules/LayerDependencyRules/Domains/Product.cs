namespace LayerDependencyRules.Domains;

public sealed class Product
{
    public string Name { get; }

    private Product(string name) => Name = name;

    public static Product Create(string name) => new(name);
}
