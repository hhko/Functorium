using TemplateDesign.Generated;

namespace TemplateDesign.Usage;

[AutoToString]
public partial class Product
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

[AutoToString]
public partial class User
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}
