using System.Diagnostics.Contracts;
using FluentValidation;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Validations;

/// <summary>
/// 값 객체 검증을 위한 FluentValidation 확장 메서드
/// </summary>
public static partial class FluentValidationExtensions
{
    /// <summary>
    /// 값 객체의 Validate 메서드를 FluentValidation 규칙으로 통합합니다.
    /// </summary>
    /// <typeparam name="TRequest">요청 타입</typeparam>
    /// <typeparam name="TProperty">속성 타입</typeparam>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="ruleBuilder">FluentValidation 규칙 빌더</param>
    /// <param name="validationMethod">값 객체의 Validate 메서드</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValueObjectValidation<TRequest, TProperty, TValueObject>(
        this IRuleBuilder<TRequest, TProperty> ruleBuilder,
        Func<TProperty, Validation<Error, TValueObject>> validationMethod)
    {
        return (IRuleBuilderOptions<TRequest, TProperty>)ruleBuilder.Custom((value, context) =>
        {
            if (value is null) return;

            var validation = validationMethod(value);
            validation.IfFail(error =>
            {
                var message = FormatErrorMessage(error);
                context.AddFailure(context.PropertyPath, message);
            });
        });
    }

    /// <summary>
    /// 값 객체의 Validate 메서드를 FluentValidation 규칙으로 통합합니다 (동일 타입용 오버로드).
    /// </summary>
    /// <typeparam name="TRequest">요청 타입</typeparam>
    /// <typeparam name="TProperty">속성 타입 (값 객체 타입과 동일)</typeparam>
    /// <param name="ruleBuilder">FluentValidation 규칙 빌더</param>
    /// <param name="validationMethod">값 객체의 Validate 메서드</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValueObjectValidation<TRequest, TProperty>(
        this IRuleBuilder<TRequest, TProperty> ruleBuilder,
        Func<TProperty, Validation<Error, TProperty>> validationMethod)
    {
        return MustSatisfyValueObjectValidation<TRequest, TProperty, TProperty>(ruleBuilder, validationMethod);
    }
}
