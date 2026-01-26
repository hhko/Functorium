using System.Diagnostics.Contracts;
using FluentValidation;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Validations;

/// <summary>
/// 값 객체 검증을 위한 FluentValidation 확장 메서드
/// </summary>
/// <remarks>
/// C# 14 extension members 문법을 사용하여 타입 추론을 개선합니다.
/// </remarks>
public static partial class FluentValidationExtensions
{
    /// <summary>
    /// Value Object의 검증 메서드를 FluentValidation 규칙으로 통합합니다.
    /// </summary>
    extension<TRequest, TProperty>(IRuleBuilder<TRequest, TProperty> ruleBuilder)
    {
        /// <summary>
        /// Value Object의 검증 메서드를 FluentValidation 규칙으로 통합합니다.
        /// <b>입력 타입과 검증 결과 타입이 동일한 경우</b> 사용합니다.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>사용 조건</b>: 검증 메서드의 입력 타입(TProperty)과 출력 타입이 동일해야 합니다.
        /// <code>Func&lt;TProperty, Validation&lt;Error, TProperty&gt;&gt;</code>
        /// </para>
        /// <para>
        /// 타입 추론이 작동하므로 명시적 타입 지정 없이 사용 가능합니다.
        /// </para>
        /// <example>
        /// <code>
        /// // decimal → Validation&lt;Error, decimal&gt; (입력 타입 == 출력 타입)
        /// RuleFor(x => x.Price)
        ///     .MustSatisfyValidation(Money.ValidateAmount);
        ///
        /// // string → Validation&lt;Error, string&gt; (입력 타입 == 출력 타입)
        /// RuleFor(x => x.Currency)
        ///     .MustSatisfyValidation(Money.ValidateCurrency);
        ///
        /// // Guid → Validation&lt;Error, Guid&gt; (입력 타입 == 출력 타입)
        /// RuleFor(x => x.ProductId)
        ///     .MustSatisfyValidation(ProductId.Validate);
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="validationMethod">
        /// Value Object의 검증 메서드. 입력 타입과 출력 타입이 동일해야 합니다.
        /// (예: Money.ValidateAmount, Money.ValidateCurrency, ProductId.Validate)
        /// </param>
        /// <returns>FluentValidation 규칙 빌더 옵션</returns>
        [Pure]
        public IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidation(
            Func<TProperty, Validation<Error, TProperty>> validationMethod)
        {
            return (IRuleBuilderOptions<TRequest, TProperty>)ruleBuilder.Custom((value, context) =>
            {
                if (value is null) return;

                validationMethod(value).IfFail(error =>
                {
                    string message = FormatErrorMessage(error);
                    context.AddFailure(context.PropertyPath, message);
                });
            });
        }

        /// <summary>
        /// Value Object의 검증 메서드를 FluentValidation 규칙으로 통합합니다.
        /// <b>입력 타입과 검증 결과 타입이 다른 경우</b> 사용합니다.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>사용 조건</b>: 검증 메서드가 입력 타입(TProperty)을 받아 다른 타입(TValueObject)으로 변환할 때 사용합니다.
        /// <code>Func&lt;TProperty, Validation&lt;Error, TValueObject&gt;&gt;</code>
        /// </para>
        /// <para>
        /// 입력 타입과 출력 타입이 다를 때 TValueObject 타입만 명시하면 됩니다.
        /// </para>
        /// <example>
        /// <code>
        /// // string → Validation&lt;Error, ProductName&gt; (입력 타입 != 출력 타입)
        /// RuleFor(x => x.Name)
        ///     .MustSatisfyValidationOf&lt;ProductName&gt;(ProductName.Validate);
        ///
        /// // string → Validation&lt;Error, Email&gt; (입력 타입 != 출력 타입)
        /// RuleFor(x => x.Email)
        ///     .MustSatisfyValidationOf&lt;Email&gt;(Email.Validate);
        /// </code>
        /// </example>
        /// </remarks>
        /// <typeparam name="TValueObject">값 객체 타입 (검증 결과 타입)</typeparam>
        /// <param name="validationMethod">Value Object의 Validate 메서드</param>
        /// <returns>FluentValidation 규칙 빌더 옵션</returns>
        [Pure]
        public IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidationOf<TValueObject>(
            Func<TProperty, Validation<Error, TValueObject>> validationMethod)
        {
            return (IRuleBuilderOptions<TRequest, TProperty>)ruleBuilder.Custom((value, context) =>
            {
                if (value is null) return;

                validationMethod(value).IfFail(error =>
                {
                    string message = FormatErrorMessage(error);
                    context.AddFailure(context.PropertyPath, message);
                });
            });
        }
    }

    /// <summary>
    /// Value Object의 검증 메서드를 FluentValidation 규칙으로 통합합니다.
    /// <b>입력 타입과 검증 결과 타입이 다른 경우</b> 사용합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>이 메서드가 필요한 이유:</b>
    /// </para>
    /// <para>
    /// C# 14 extension members는 추가 제네릭 타입 파라미터가 있을 때 파생 인터페이스에서
    /// 타입 추론이 작동하지 않는 제한이 있습니다.
    /// </para>
    /// <para>
    /// <b>문제 상황 예제:</b>
    /// <code>
    /// // C# 14 extension block 정의
    /// extension&lt;TRequest, TProperty&gt;(IRuleBuilder&lt;TRequest, TProperty&gt; ruleBuilder)
    /// {
    ///     // 추가 제네릭 파라미터 없음 → 정상 작동
    ///     public IRuleBuilderOptions&lt;TRequest, TProperty&gt; MustSatisfyValidation(
    ///         Func&lt;TProperty, Validation&lt;Error, TProperty&gt;&gt; validationMethod) { ... }
    ///
    ///     // 추가 제네릭 파라미터 있음 → IRuleBuilderInitial에서 호출 불가
    ///     public IRuleBuilderOptions&lt;TRequest, TProperty&gt; MustSatisfyValidationOf&lt;TValueObject&gt;(
    ///         Func&lt;TProperty, Validation&lt;Error, TValueObject&gt;&gt; validationMethod) { ... }
    /// }
    ///
    /// // 사용 시:
    /// public class Validator : AbstractValidator&lt;Request&gt;
    /// {
    ///     public Validator()
    ///     {
    ///         // RuleFor()는 IRuleBuilderInitial&lt;Request, string&gt; 반환
    ///         // IRuleBuilderInitial은 IRuleBuilder를 상속
    ///
    ///         // ✅ 작동: 추가 제네릭 파라미터 없음
    ///         RuleFor(x => x.Currency)
    ///             .MustSatisfyValidation(Money.ValidateCurrency);
    ///
    ///         // ❌ 컴파일 에러: CS1061 - 'IRuleBuilderInitial&lt;Request, string&gt;'에는
    ///         //    'MustSatisfyValidationOf'에 대한 정의가 포함되어 있지 않습니다.
    ///         RuleFor(x => x.Age)
    ///             .MustSatisfyValidationOf&lt;int&gt;(Age.Validate);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <b>원인:</b> C# 14 extension members가 생성하는 확장 메서드는 <c>IRuleBuilder</c>를
    /// 대상으로 하지만, 추가 제네릭 파라미터가 있으면 컴파일러가 파생 인터페이스
    /// (<c>IRuleBuilderInitial</c>)에서 메서드를 해결하지 못합니다.
    /// </para>
    /// <para>
    /// <b>해결책:</b>
    /// <c>IRuleBuilderInitial</c>을 직접 대상으로 하는 전통적인 확장 메서드를 제공하여
    /// <c>RuleFor()</c> 호출 결과에서 직접 사용할 수 있도록 합니다.
    /// </para>
    /// <para>
    /// <b>참고:</b> extension block 내의 동일 메서드는 <c>IRuleBuilder</c> 타입 변수를
    /// 직접 사용하는 드문 경우를 위해 유지됩니다.
    /// </para>
    /// <example>
    /// <code>
    /// // string → Validation&lt;Error, int&gt; (입력 타입 != 출력 타입)
    /// // 모든 타입 파라미터 명시 필요: TRequest, TProperty, TValueObject
    /// RuleFor(x => x.Age)
    ///     .MustSatisfyValidationOf&lt;TestAgeRequest, string, int&gt;(Age.Validate);
    /// </code>
    /// </example>
    /// </remarks>
    /// <typeparam name="TRequest">Request 타입</typeparam>
    /// <typeparam name="TProperty">속성 타입 (입력 타입)</typeparam>
    /// <typeparam name="TValueObject">값 객체 타입 (검증 결과 타입)</typeparam>
    /// <param name="ruleBuilder">Rule builder</param>
    /// <param name="validationMethod">Value Object의 Validate 메서드</param>
    /// <returns>FluentValidation 규칙 빌더 옵션</returns>
    [Pure]
    public static IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidationOf<TRequest, TProperty, TValueObject>(
        this IRuleBuilderInitial<TRequest, TProperty> ruleBuilder,
        Func<TProperty, Validation<Error, TValueObject>> validationMethod)
    {
        return (IRuleBuilderOptions<TRequest, TProperty>)ruleBuilder.Custom((value, context) =>
        {
            if (value is null) return;

            validationMethod(value).IfFail(error =>
            {
                string message = FormatErrorMessage(error);
                context.AddFailure(context.PropertyPath, message);
            });
        });
    }
}
