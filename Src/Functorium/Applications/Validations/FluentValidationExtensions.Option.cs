using System.Diagnostics.Contracts;
using FluentValidation;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Validations;

/// <summary>
/// Option&lt;T&gt; 속성을 위한 FluentValidation 확장 메서드.
/// None이면 자동 스킵, Some이면 내부 값으로 검증을 실행합니다.
/// </summary>
public static partial class FluentValidationExtensions
{
    extension<TRequest, TProperty>(IRuleBuilder<TRequest, Option<TProperty>> ruleBuilder)
    {
        /// <summary>
        /// Option&lt;TProperty&gt; 속성에 대해 Value Object의 검증 메서드를 적용합니다.
        /// None이면 검증을 건너뛰고, Some이면 내부 값을 추출하여 검증합니다.
        /// <b>입력 타입과 검증 결과 타입이 동일한 경우</b> 사용합니다.
        /// </summary>
        /// <example>
        /// <code>
        /// // Option&lt;decimal&gt; → Validation&lt;Error, decimal&gt;
        /// RuleFor(x => x.MinPrice)          // Option&lt;decimal&gt;
        ///     .MustSatisfyValidation(Money.Validate);  // None → skip, Some(100m) → validate
        /// </code>
        /// </example>
        [Pure]
        public IRuleBuilderOptions<TRequest, Option<TProperty>> MustSatisfyValidation(
            Func<TProperty, Validation<Error, TProperty>> validationMethod)
        {
            return (IRuleBuilderOptions<TRequest, Option<TProperty>>)ruleBuilder.Custom((value, context) =>
            {
                value.Iter(inner =>
                {
                    validationMethod(inner).IfFail(error =>
                    {
                        string message = FormatErrorMessage(error);
                        context.AddFailure(context.PropertyPath, message);
                    });
                });
            });
        }

        /// <summary>
        /// Option&lt;TProperty&gt; 속성에 대해 Value Object의 검증 메서드를 적용합니다.
        /// None이면 검증을 건너뛰고, Some이면 내부 값을 추출하여 검증합니다.
        /// <b>입력 타입과 검증 결과 타입이 다른 경우</b> 사용합니다.
        /// </summary>
        /// <example>
        /// <code>
        /// // Option&lt;string&gt; → Validation&lt;Error, ProductName&gt;
        /// RuleFor(x => x.Name)                                      // Option&lt;string&gt;
        ///     .MustSatisfyValidationOf&lt;ProductName&gt;(ProductName.Validate);  // None → skip
        /// </code>
        /// </example>
        [Pure]
        public IRuleBuilderOptions<TRequest, Option<TProperty>> MustSatisfyValidationOf<TValueObject>(
            Func<TProperty, Validation<Error, TValueObject>> validationMethod)
        {
            return (IRuleBuilderOptions<TRequest, Option<TProperty>>)ruleBuilder.Custom((value, context) =>
            {
                value.Iter(inner =>
                {
                    validationMethod(inner).IfFail(error =>
                    {
                        string message = FormatErrorMessage(error);
                        context.AddFailure(context.PropertyPath, message);
                    });
                });
            });
        }
    }

    /// <summary>
    /// Option&lt;TProperty&gt; 속성에 대해 Value Object의 검증 메서드를 적용합니다.
    /// IRuleBuilderInitial에서 추가 제네릭 파라미터가 있는 메서드를 호출할 수 있도록
    /// 전통적인 확장 메서드로 제공합니다.
    /// </summary>
    [Pure]
    public static IRuleBuilderOptions<TRequest, Option<TProperty>> MustSatisfyValidationOf<TRequest, TProperty, TValueObject>(
        this IRuleBuilderInitial<TRequest, Option<TProperty>> ruleBuilder,
        Func<TProperty, Validation<Error, TValueObject>> validationMethod)
    {
        return (IRuleBuilderOptions<TRequest, Option<TProperty>>)ruleBuilder.Custom((value, context) =>
        {
            value.Iter(inner =>
            {
                validationMethod(inner).IfFail(error =>
                {
                    string message = FormatErrorMessage(error);
                    context.AddFailure(context.PropertyPath, message);
                });
            });
        });
    }
}
