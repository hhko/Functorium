using EntityIdGenerator.Usage;

var orderId = OrderId.New();
var productId = ProductId.New();

Console.WriteLine($"OrderId: {orderId}");
Console.WriteLine($"ProductId: {productId}");

var sameId = OrderId.From(orderId.Value);
Console.WriteLine($"orderId == sameId: {orderId == sameId}");
Console.WriteLine($"orderId == ProductId.New(): Type safety prevents comparison at compile time!");
