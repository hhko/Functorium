using CleanArchitecture.Domain.Exceptions;

namespace CleanArchitecture.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email is required");

        if (!value.Contains('@') || !value.Contains('.'))
            throw new DomainException("Invalid email format");

        Value = value.ToLowerInvariant();
    }
}
