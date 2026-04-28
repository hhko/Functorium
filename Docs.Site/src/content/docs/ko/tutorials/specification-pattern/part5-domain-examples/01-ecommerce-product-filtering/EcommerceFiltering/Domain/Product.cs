using EcommerceFiltering.Domain.ValueObjects;

namespace EcommerceFiltering.Domain;

public record Product(ProductName Name, Money Price, Quantity Stock, Category Category);
