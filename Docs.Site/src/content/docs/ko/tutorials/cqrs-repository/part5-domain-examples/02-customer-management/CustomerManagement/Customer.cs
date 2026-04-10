using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace CustomerManagement;

/// <summary>
/// 고객 Aggregate Root.
/// IAuditable을 구현하여 생성/수정 시각을 추적합니다.
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    // ─── Domain Events ──────────────────────────────────
    public sealed record CustomerCreatedEvent(CustomerId CustomerId, string Name) : DomainEvent;
    public sealed record CreditLimitUpdatedEvent(CustomerId CustomerId, decimal OldLimit, decimal NewLimit) : DomainEvent;
    public sealed record EmailChangedEvent(CustomerId CustomerId, string OldEmail, string NewEmail) : DomainEvent;

    // ─── Properties ─────────────────────────────────────
    public string Name { get; private set; }
    public string Email { get; private set; }
    public decimal CreditLimit { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    private Customer(CustomerId id, string name, string email, decimal creditLimit)
    {
        Id = id;
        Name = name;
        Email = email;
        CreditLimit = creditLimit;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = None;
    }

    // ─── Factory ────────────────────────────────────────

    public static Fin<Customer> Create(string name, string email, decimal creditLimit = 0m)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.New("Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            return Error.New("Email is required.");

        if (creditLimit < 0)
            return Error.New("CreditLimit cannot be negative.");

        var customer = new Customer(CustomerId.New(), name, email, creditLimit);
        customer.AddDomainEvent(new CustomerCreatedEvent(customer.Id, name));
        return Fin.Succ(customer);
    }

    // ─── Commands ───────────────────────────────────────

    public Fin<Unit> UpdateCreditLimit(decimal newLimit)
    {
        if (newLimit < 0)
            return Error.New("CreditLimit cannot be negative.");

        var oldLimit = CreditLimit;
        CreditLimit = newLimit;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CreditLimitUpdatedEvent(Id, oldLimit, newLimit));
        return unit;
    }

    public Fin<Unit> ChangeEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            return Error.New("Email is required.");

        var oldEmail = Email;
        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new EmailChangedEvent(Id, oldEmail, newEmail));
        return unit;
    }
}
