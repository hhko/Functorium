using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;

internal static class CustomerMapper
{
    public static CustomerModel ToModel(this Customer customer) => new()
    {
        Id = customer.Id.ToString(),
        Name = customer.Name,
        Email = customer.Email,
        CreditLimit = customer.CreditLimit,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt
    };

    public static Customer ToDomain(this CustomerModel model) =>
        Customer.CreateFromValidated(
            CustomerId.Create(model.Id),
            CustomerName.CreateFromValidated(model.Name),
            Email.CreateFromValidated(model.Email),
            Money.CreateFromValidated(model.CreditLimit),
            model.CreatedAt,
            model.UpdatedAt);
}
