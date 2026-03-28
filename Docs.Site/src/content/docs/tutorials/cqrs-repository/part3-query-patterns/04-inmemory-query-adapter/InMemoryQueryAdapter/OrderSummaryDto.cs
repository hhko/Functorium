namespace InMemoryQueryAdapter;

public sealed record OrderSummaryDto(
    string OrderId,
    string ProductName,
    string Category,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount);
