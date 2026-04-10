using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;

namespace AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

/// <summary>
/// 배포 엔드포인트 URL 값 객체
/// </summary>
public sealed class EndpointUrl : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidUri : DomainErrorType.Custom;

    #endregion

    private EndpointUrl(string value) : base(value) { }

    public static Fin<EndpointUrl> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new EndpointUrl(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<EndpointUrl>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMust(
                v => Uri.TryCreate(v, UriKind.Absolute, out var uri)
                     && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                new InvalidUri(),
                v => $"Invalid endpoint URL format: '{v}'");

    public static EndpointUrl CreateFromValidated(string value) => new(value);

    public static implicit operator string(EndpointUrl url) => url.Value;
}
