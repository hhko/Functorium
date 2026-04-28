using ForAttribute.Generated;

namespace ForAttribute.Usage;

[AutoDescribe]
public partial class Product
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
}

[AutoDescribe]
public partial class Customer
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
}
