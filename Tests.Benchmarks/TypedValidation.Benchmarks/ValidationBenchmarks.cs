using BenchmarkDotNet.Attributes;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using LanguageExt;
using LanguageExt.Common;
using System.Text.RegularExpressions;

namespace TypedValidation.Benchmarks;

// Mock value object types for testing
public sealed class Email;
public sealed class Username;
public sealed class Price;

/// <summary>
/// Benchmark comparing ValidationRules (old) vs Validate&lt;T&gt; (new) patterns
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public partial class ValidationBenchmarks
{
    private static readonly Regex EmailPattern = EmailRegex();

    // Test data
    private const string ValidEmail = "test@example.com";
    private const string ValidUsername = "john_doe123";
    private const decimal ValidPrice = 99.99m;

    private const string InvalidEmail = "";
    private const string TooLongEmail = "this.is.a.very.long.email.address.that.exceeds.the.maximum.allowed.length@example.com.with.even.more.subdomains.to.make.it.really.really.really.really.really.really.really.really.really.really.really.long.com";

    #region Scenario 1: Simple validation (single step)

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Simple")]
    public Validation<Error, decimal> Old_SimpleValidation_Success()
    {
        return ValidationRules.Positive<Price, decimal>(ValidPrice);
    }

    [Benchmark]
    [BenchmarkCategory("Simple")]
    public Validation<Error, decimal> New_SimpleValidation_Success()
    {
        return ValidationRules<Price>.Positive(ValidPrice);
    }

    [Benchmark]
    [BenchmarkCategory("Simple")]
    public Validation<Error, decimal> Old_SimpleValidation_Failure()
    {
        return ValidationRules.Positive<Price, decimal>(-1m);
    }

    [Benchmark]
    [BenchmarkCategory("Simple")]
    public Validation<Error, decimal> New_SimpleValidation_Failure()
    {
        return ValidationRules<Price>.Positive(-1m);
    }

    #endregion

    #region Scenario 2: Chaining (3 steps)

    [Benchmark]
    [BenchmarkCategory("Chaining3")]
    public Validation<Error, string> Old_Chain3_Success()
    {
        return ValidationRules.NotEmpty<Email>(ValidEmail)
            .ThenMatches<Email>(EmailPattern)
            .ThenMaxLength<Email>(254);
    }

    [Benchmark]
    [BenchmarkCategory("Chaining3")]
    public Validation<Error, string> New_Chain3_Success()
    {
        return ValidationRules<Email>.NotEmpty(ValidEmail)
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254);
    }

    [Benchmark]
    [BenchmarkCategory("Chaining3")]
    public Validation<Error, string> Old_Chain3_Failure_First()
    {
        return ValidationRules.NotEmpty<Email>(InvalidEmail)
            .ThenMatches<Email>(EmailPattern)
            .ThenMaxLength<Email>(254);
    }

    [Benchmark]
    [BenchmarkCategory("Chaining3")]
    public Validation<Error, string> New_Chain3_Failure_First()
    {
        return ValidationRules<Email>.NotEmpty(InvalidEmail)
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254);
    }

    [Benchmark]
    [BenchmarkCategory("Chaining3")]
    public Validation<Error, string> Old_Chain3_Failure_Last()
    {
        return ValidationRules.NotEmpty<Email>(TooLongEmail)
            .ThenMatches<Email>(EmailPattern)
            .ThenMaxLength<Email>(254);
    }

    [Benchmark]
    [BenchmarkCategory("Chaining3")]
    public Validation<Error, string> New_Chain3_Failure_Last()
    {
        return ValidationRules<Email>.NotEmpty(TooLongEmail)
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254);
    }

    #endregion

    #region Scenario 3: Complex chaining (5 steps + normalization)

    [Benchmark]
    [BenchmarkCategory("Chaining5")]
    public Validation<Error, string> Old_Chain5_Success()
    {
        return ValidationRules.NotEmpty<Email>(ValidEmail)
            .ThenMatches<Email>(EmailPattern)
            .ThenMinLength<Email>(5)
            .ThenMaxLength<Email>(254)
            .ThenNormalize(v => v.ToLowerInvariant());
    }

    [Benchmark]
    [BenchmarkCategory("Chaining5")]
    public Validation<Error, string> New_Chain5_Success()
    {
        return ValidationRules<Email>.NotEmpty(ValidEmail)
            .ThenMatches(EmailPattern)
            .ThenMinLength(5)
            .ThenMaxLength(254)
            .ThenNormalize(v => v.ToLowerInvariant());
    }

    [Benchmark]
    [BenchmarkCategory("Chaining5")]
    public Validation<Error, string> Old_Chain5_Failure_First()
    {
        return ValidationRules.NotEmpty<Email>(InvalidEmail)
            .ThenMatches<Email>(EmailPattern)
            .ThenMinLength<Email>(5)
            .ThenMaxLength<Email>(254)
            .ThenNormalize(v => v.ToLowerInvariant());
    }

    [Benchmark]
    [BenchmarkCategory("Chaining5")]
    public Validation<Error, string> New_Chain5_Failure_First()
    {
        return ValidationRules<Email>.NotEmpty(InvalidEmail)
            .ThenMatches(EmailPattern)
            .ThenMinLength(5)
            .ThenMaxLength(254)
            .ThenNormalize(v => v.ToLowerInvariant());
    }

    #endregion

    #region Scenario 4: Numeric chaining

    [Benchmark]
    [BenchmarkCategory("Numeric")]
    public Validation<Error, decimal> Old_NumericChain_Success()
    {
        return ValidationRules.Positive<Price, decimal>(ValidPrice)
            .ThenAtMost<Price, decimal>(1_000_000m);
    }

    [Benchmark]
    [BenchmarkCategory("Numeric")]
    public Validation<Error, decimal> New_NumericChain_Success()
    {
        return ValidationRules<Price>.Positive(ValidPrice)
            .ThenAtMost(1_000_000m);
    }

    #endregion

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
