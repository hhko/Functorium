using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace MyApp.Domain;

/// <summary>
/// Email Value Object with stricter (RFC-inspired) validation.
/// This is not a full RFC 5322 parser, but it's significantly stricter than a naive check.
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

        // Length constraints (RFC 5321 guidance)
        if (trimmed.Length > 254)
            throw new ArgumentException("Email must be at most 254 characters.", nameof(raw));

        // Basic parse using MailAddress (handles many edge cases better than regex-only)
        MailAddress addr;
        try
        {
            addr = new MailAddress(trimmed);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email format is invalid.", nameof(raw));
        }

        // Ensure exact (no display name, no angle brackets formatting quirks)
        if (!string.Equals(addr.Address, trimmed, StringComparison.Ordinal))
            throw new ArgumentException("Email must be a plain address (no display name).", nameof(raw));

        var at = trimmed.IndexOf('@');
        if (at <= 0 || at != trimmed.LastIndexOf('@') || at == trimmed.Length - 1)
            throw new ArgumentException("Email must contain a single '@' with local and domain parts.", nameof(raw));

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];

        // Local-part length <= 64
        if (local.Length > 64)
            throw new ArgumentException("Local part must be at most 64 characters.", nameof(raw));

        // Disallow consecutive dots and leading/trailing dot in local
        if (local.StartsWith('.') || local.EndsWith('.') || local.Contains(".."))
            throw new ArgumentException("Local part cannot start/end with '.' or contain consecutive dots.", nameof(raw));

        // Local-part: allow common atext + dot (no quotes in this simplified stricter model)
        // atext per RFC 5322: ALPHA / DIGIT / "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" / "|" / "}" / "~"
        // We also allow '.' as separator (handled above for dot rules)
        var localRegex = new Regex(@"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~.-]+$", RegexOptions.CultureInvariant);
        if (!localRegex.IsMatch(local))
            throw new ArgumentException("Local part contains invalid characters (quotes/spaces not allowed).", nameof(raw));

        // Normalize domain: IDN (punycode) then validate labels
        domain = NormalizeDomainToAscii(domain);

        // Domain length <= 253 (practical)
        if (domain.Length > 253)
            throw new ArgumentException("Domain part is too long.", nameof(raw));

        ValidateDomainLabels(domain, raw);

        // Normalization policy for uniqueness: uppercase invariant of full address with ASCII domain
        var normalized = (local + "@" + domain).ToUpperInvariant();

        // Value stored: keep user's trimmed input, but with ASCII domain to avoid ambiguity
        var value = local + "@" + domain;

        return new Email(value, normalized);
    }

    private static string NormalizeDomainToAscii(string domain)
    {
        // Strip a trailing dot (FQDN form) for stability
        domain = domain.TrimEnd('.');

        // Convert Unicode domain to ASCII via IDN
        var idn = new IdnMapping();
        try
        {
            // Convert each label; IdnMapping.GetAscii works on full domain too
            return idn.GetAscii(domain);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException("Domain part is invalid (IDN conversion failed).", nameof(domain));
        }
    }

    private static void ValidateDomainLabels(string domain, string rawParamName)
    {
        // Must have at least one dot (optional, but commonly enforced). If you want to allow single-label domains, remove this.
        if (!domain.Contains('.'))
            throw new ArgumentException("Domain must contain at least one dot.", nameof(rawParamName));

        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (var label in labels)
        {
            // Each label 1..63
            if (label.Length is < 1 or > 63)
                throw new ArgumentException("Domain label length must be 1..63.", nameof(rawParamName));

            // Labels: [A-Za-z0-9-], no leading/trailing '-', no consecutive '.' already handled
            if (label.StartsWith('-') || label.EndsWith('-'))
                throw new ArgumentException("Domain labels cannot start/end with '-'.", nameof(rawParamName));

            if (!Regex.IsMatch(label, @"^[A-Za-z0-9-]+$", RegexOptions.CultureInvariant))
                throw new ArgumentException("Domain contains invalid characters.", nameof(rawParamName));
        }

        // TLD must be alpha (common policy; if you need numeric TLDs or internal domains, relax this)
        var tld = labels[^1];
        if (!Regex.IsMatch(tld, @"^[A-Za-z]{2,63}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Top-level domain must be alphabetic (2..63).", nameof(rawParamName));
    }

    public override string ToString() => Value;
}
