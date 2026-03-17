using Functorium.Domains.ValueObjects.Validations.Contextual;
using LanguageExt;
using LanguageExt.Common;

namespace ContextualValidation.Examples;

public sealed record AddressDto(string City, string Street, string PostalCode);

public static class AddressValidation
{
    /// <summary>
    /// 다중 필드 ContextualValidation — Apply 조합으로 모든 오류를 수집합니다.
    /// </summary>
    public static Validation<Error, AddressDto> Validate(string? city, string? street, string? postalCode)
        => (ValidateCity(city), ValidateStreet(street), ValidatePostalCode(postalCode))
            .Apply((c, s, p) => new AddressDto(c, s, p));

    private static Validation<Error, string> ValidateCity(string? value)
        => ValidationRules.For("City")
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(100);

    private static Validation<Error, string> ValidateStreet(string? value)
        => ValidationRules.For("Street")
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(200);

    private static Validation<Error, string> ValidatePostalCode(string? value)
        => ValidationRules.For("PostalCode")
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMinLength(5)
            .ThenMaxLength(10);
}
