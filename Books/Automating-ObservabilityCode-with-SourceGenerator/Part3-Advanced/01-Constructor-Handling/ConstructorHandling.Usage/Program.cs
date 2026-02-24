using ConstructorHandling.Usage;

var order = Order.Create("ORD-001", "Alice", 3);
Console.WriteLine($"Order: {order.OrderId}, Customer: {order.CustomerName}, Items: {order.ItemCount}");

var product = Product.Create("Laptop", 999.99m);
Console.WriteLine($"Product: {product.Name}, Price: {product.Price}");
