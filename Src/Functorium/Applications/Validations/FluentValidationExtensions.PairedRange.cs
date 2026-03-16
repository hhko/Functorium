using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Validations;

/// <summary>
/// 쌍 범위(paired range) 검증을 위한 FluentValidation 확장 메서드.
/// MinPrice/MaxPrice처럼 반드시 함께 제공되어야 하는 Option 쌍 필터를 단일 호출로 검증합니다.
/// </summary>
public static partial class FluentValidationExtensions
{
    /// <summary>
    /// 두 Option 필드가 반드시 함께 제공되어야 하는 쌍 범위 필터를 검증합니다.
    /// </summary>
    /// <remarks>
    /// <para>내부 동작:</para>
    /// <list type="number">
    /// <item>둘 다 None → 통과 (필터 미적용)</item>
    /// <item>하나만 Some → 실패 ("MinPrice and MaxPrice must be provided together")</item>
    /// <item>둘 다 Some → 각각 validate 실행 + 범위 검증</item>
    /// </list>
    /// <example>
    /// <code>
    /// // 기본: max > min (exclusive)
    /// this.MustBePairedRange(
    ///     x => x.MinPrice,
    ///     x => x.MaxPrice,
    ///     Money.Validate);
    ///
    /// // 커스텀: max >= min (inclusive)
    /// this.MustBePairedRange(
    ///     x => x.MinPrice,
    ///     x => x.MaxPrice,
    ///     Money.Validate,
    ///     inclusive: true);
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="validator">Validator 인스턴스</param>
    /// <param name="minExpr">최솟값 속성 표현식</param>
    /// <param name="maxExpr">최댓값 속성 표현식</param>
    /// <param name="validate">값 검증 메서드</param>
    /// <param name="inclusive">true: max >= min, false(기본): max > min</param>
    public static void MustBePairedRange<TRequest, T>(
        this AbstractValidator<TRequest> validator,
        Expression<Func<TRequest, Option<T>>> minExpr,
        Expression<Func<TRequest, Option<T>>> maxExpr,
        Func<T, Validation<Error, T>> validate,
        bool inclusive = false)
        where T : IComparable<T>
    {
        var minCompiled = minExpr.Compile();
        var maxCompiled = maxExpr.Compile();
        string minName = GetMemberName(minExpr);
        string maxName = GetMemberName(maxExpr);

        validator.RuleFor(minExpr).Custom((_, context) =>
        {
            var request = (TRequest)context.InstanceToValidate;
            var minOption = minCompiled(request);
            var maxOption = maxCompiled(request);

            switch (minOption.IsSome, maxOption.IsSome)
            {
                case (false, false):
                    return;

                case (true, false):
                    context.AddFailure(maxName,
                        $"{maxName} is required when {minName} is specified");
                    return;

                case (false, true):
                    context.AddFailure(minName,
                        $"{minName} is required when {maxName} is specified");
                    return;

                case (true, true):
                    break;
            }

            var minVal = minOption.Match(v => v, () => default!);
            var maxVal = maxOption.Match(v => v, () => default!);

            validate(minVal).IfFail(error =>
                context.AddFailure(minName, FormatErrorMessage(error)));

            validate(maxVal).IfFail(error =>
                context.AddFailure(maxName, FormatErrorMessage(error)));

            int cmp = maxVal.CompareTo(minVal);
            if (inclusive ? cmp < 0 : cmp <= 0)
            {
                string op = inclusive ? "greater than or equal to" : "greater than";
                context.AddFailure(maxName, $"{maxName} must be {op} {minName}");
            }
        });
    }

    private static string GetMemberName<TRequest, T>(Expression<Func<TRequest, T>> expression)
    {
        return expression.Body is MemberExpression memberExpr
            ? memberExpr.Member.Name
            : throw new ArgumentException("Expression must be a member access expression");
    }
}
