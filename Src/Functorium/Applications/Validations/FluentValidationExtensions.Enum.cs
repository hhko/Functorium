using System.Diagnostics.Contracts;
using Ardalis.SmartEnum;
using FluentValidation;

namespace Functorium.Applications.Validations;

/// <summary>
/// SmartEnum 검증을 위한 FluentValidation 확장 메서드
/// </summary>
public static partial class FluentValidationExtensions
{
    /// <summary>
    /// SmartEnum 값 객체에 대한 검증 (Value로 검증)
    /// </summary>
    /// <typeparam name="TRequest">요청 타입</typeparam>
    /// <typeparam name="TSmartEnum">SmartEnum 타입</typeparam>
    /// <typeparam name="TValue">SmartEnum의 Value 타입</typeparam>
    /// <param name="ruleBuilder">FluentValidation 규칙 빌더</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, TValue> MustBeEnum<TRequest, TSmartEnum, TValue>(
        this IRuleBuilder<TRequest, TValue> ruleBuilder)
        where TSmartEnum : SmartEnum<TSmartEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        return (IRuleBuilderOptions<TRequest, TValue>)ruleBuilder.Custom((value, context) =>
        {
            if (value is null) return;

            if (!SmartEnum<TSmartEnum, TValue>.TryFromValue(value, out _))
            {
                context.AddFailure(context.PropertyPath,
                    $"'{value}' is not a valid {typeof(TSmartEnum).Name}");
            }
        });
    }

    /// <summary>
    /// SmartEnum 값 객체에 대한 검증 (Name으로 검증)
    /// </summary>
    /// <typeparam name="TRequest">요청 타입</typeparam>
    /// <typeparam name="TSmartEnum">SmartEnum 타입</typeparam>
    /// <typeparam name="TValue">SmartEnum의 Value 타입</typeparam>
    /// <param name="ruleBuilder">FluentValidation 규칙 빌더</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, string> MustBeEnumName<TRequest, TSmartEnum, TValue>(
        this IRuleBuilder<TRequest, string> ruleBuilder)
        where TSmartEnum : SmartEnum<TSmartEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        return (IRuleBuilderOptions<TRequest, string>)ruleBuilder.Custom((value, context) =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            if (!SmartEnum<TSmartEnum, TValue>.TryFromName(value, out _))
            {
                context.AddFailure(context.PropertyPath,
                    $"'{value}' is not a valid {typeof(TSmartEnum).Name} name");
            }
        });
    }

    /// <summary>
    /// int 기반 SmartEnum 값 객체에 대한 간소화 오버로드
    /// </summary>
    /// <typeparam name="TRequest">요청 타입</typeparam>
    /// <typeparam name="TSmartEnum">SmartEnum 타입</typeparam>
    /// <param name="ruleBuilder">FluentValidation 규칙 빌더</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, int> MustBeEnum<TRequest, TSmartEnum>(
        this IRuleBuilder<TRequest, int> ruleBuilder)
        where TSmartEnum : SmartEnum<TSmartEnum, int>
    {
        return MustBeEnum<TRequest, TSmartEnum, int>(ruleBuilder);
    }
}
