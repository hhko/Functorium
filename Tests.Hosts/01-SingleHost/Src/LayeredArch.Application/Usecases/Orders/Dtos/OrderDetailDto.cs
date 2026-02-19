namespace LayeredArch.Application.Usecases.Orders.Dtos;

public sealed record OrderDetailDto(
    string OrderId,
    string ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string ShippingAddress,
    DateTime CreatedAt);
