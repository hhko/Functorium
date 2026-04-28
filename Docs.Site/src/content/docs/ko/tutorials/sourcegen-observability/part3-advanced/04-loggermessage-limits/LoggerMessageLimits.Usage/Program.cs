using LoggerMessageLimits.Usage;

var service = new OrderService();

// Within 6-param limit
var msg1 = OrderService.ProcessOrderLogMessage("ORD-001", "Alice", 99.99m);
Console.WriteLine($"Within limit (3 params): {msg1}");

// Exceeds 6-param limit — fallback strategy
var msg2 = OrderService.AuditOrderLogMessage("ORD-001", "Alice", 99.99m, "2024-01-01", "USD", "pending", "warehouse-1");
Console.WriteLine($"Exceeds limit (7 params): {msg2}");
