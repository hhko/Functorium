using ForAttribute.Usage;

var product = new Product { Name = "Laptop", Price = 999.99m, Category = "Electronics" };
Console.WriteLine(product.Describe());

var customer = new Customer { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
Console.WriteLine(customer.Describe());
