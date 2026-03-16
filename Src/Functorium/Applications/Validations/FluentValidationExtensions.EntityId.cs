using System.Diagnostics.Contracts;
using FluentValidation;
using Functorium.Domains.Entities;

namespace Functorium.Applications.Validations;

/// <summary>
/// EntityId 검증을 위한 FluentValidation 확장 메서드
/// </summary>
public static partial class FluentValidationExtensions
{
    /// <summary>
    /// 문자열 속성이 유효한 EntityId 형식인지 검증합니다.
    /// NotEmpty + TryParse를 하나의 규칙으로 통합합니다.
    /// </summary>
    /// <typeparam name="TRequest">요청 타입</typeparam>
    /// <typeparam name="TEntityId">EntityId 타입</typeparam>
    /// <param name="ruleBuilder">FluentValidation 규칙 빌더</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, string> MustBeEntityId<TRequest, TEntityId>(
        this IRuleBuilder<TRequest, string> ruleBuilder)
        where TEntityId : struct, IEntityId<TEntityId>
    {
        return (IRuleBuilderOptions<TRequest, string>)ruleBuilder.Custom((value, context) =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                context.AddFailure(context.PropertyPath,
                    $"'{context.PropertyPath}' must not be empty");
                return;
            }

            if (!TEntityId.TryParse(value, null, out _))
            {
                context.AddFailure(context.PropertyPath,
                    $"'{value}' is not a valid {typeof(TEntityId).Name} format");
            }
        });
    }
}
