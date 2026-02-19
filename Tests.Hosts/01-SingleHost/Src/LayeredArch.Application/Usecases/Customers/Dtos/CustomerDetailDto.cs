namespace LayeredArch.Application.Usecases.Customers.Dtos;

public sealed record CustomerDetailDto(
    string CustomerId,
    string Name,
    string Email,
    decimal CreditLimit,
    DateTime CreatedAt);
