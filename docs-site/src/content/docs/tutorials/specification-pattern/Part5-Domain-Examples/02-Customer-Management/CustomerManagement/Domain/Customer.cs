using CustomerManagement.Domain.ValueObjects;

namespace CustomerManagement.Domain;

public record Customer(CustomerId Id, CustomerName Name, Email Email, bool IsActive);
