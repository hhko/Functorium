namespace MyApp.Domain;

/// <summary>
/// Email Value Object.
/// - Value: original(normalized trimming) representation used by the domain
/// - Normalized: storage / comparison-friendly representation for uniqueness
/// </summary>
public sealed class Email
{
    public string Value { get; }
    public string Normalized { get; }

    private Email(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Email is required.", nameof(raw));

        var trimmed = raw.Trim();

        // Minimal validation (keep it simple; replace with robust validation if needed)
        if (!trimmed.Contains('@') || trimmed.StartsWith('@') || trimmed.EndsWith('@'))
            throw new ArgumentException("Email format is invalid.", nameof(raw));

        // Normalization policy: upper invariant (could be lower; must be consistent everywhere)
        var normalized = trimmed.ToUpperInvariant();

        return new Email(trimmed, normalized);
    }

    public override string ToString() => Value;
}
