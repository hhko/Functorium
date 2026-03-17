using Functorium.Domains.ValueObjects.Validations.Contextual;
using LanguageExt;
using LanguageExt.Common;

namespace ContextualValidation.Examples;

public static class PhoneNumberValidation
{
    /// <summary>
    /// ContextualValidation으로 전화번호를 검증합니다.
    /// 필드 이름 기반으로 에러 메시지에 컨텍스트가 포함됩니다.
    /// </summary>
    public static Validation<Error, string> Validate(string? phoneNumber)
        => ValidationRules.For("PhoneNumber")
            .NotNull(phoneNumber)
            .ThenNotEmpty()
            .ThenMinLength(10)
            .ThenMaxLength(15);
}
