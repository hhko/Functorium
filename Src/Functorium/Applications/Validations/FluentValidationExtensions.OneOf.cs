using System.Diagnostics.Contracts;
using FluentValidation;

namespace Functorium.Applications.Validations;

public static partial class FluentValidationExtensions
{
    /// <summary>
    /// 허용된 문자열 목록 중 하나인지 검증합니다 (대소문자 무시).
    /// null 또는 빈 문자열은 검증을 건너뜁니다.
    /// </summary>
    [Pure]
    public static IRuleBuilderOptions<TRequest, string> MustBeOneOf<TRequest>(
        this IRuleBuilder<TRequest, string> ruleBuilder,
        string[] allowedValues)
    {
        return (IRuleBuilderOptions<TRequest, string>)ruleBuilder.Custom((value, context) =>
        {
            if (string.IsNullOrEmpty(value)) return;

            if (!allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                context.AddFailure(context.PropertyPath,
                    $"'{context.PropertyPath}' must be one of: {string.Join(", ", allowedValues)}");
            }
        });
    }
}
