using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

/// <summary>
/// Named Context 검증을 위한 진입점
/// </summary>
/// <remarks>
/// <para>
/// Value Object 없이 primitive 타입을 검증할 때 사용합니다.
/// </para>
/// <para>
/// <b>사용 예시:</b>
/// <code>
/// // Named Context 검증
/// ValidationRules.For("ProductRegistration").Positive(amount);
/// // Error: DomainErrors.ProductRegistration.NotPositive
///
/// // 체이닝
/// ValidationRules.For("OrderValidation")
///     .NotEmpty(name)
///     .ThenMinLength(3)
///     .ThenMaxLength(100);
/// </code>
/// </para>
/// <para>
/// <b>DDD 관점:</b>
/// <list type="bullet">
/// <item>Domain Layer: Value Object 사용 권장 (<c>ValidationRules&lt;Price&gt;</c>)</item>
/// <item>Application Layer: Context Class 사용 (<c>ValidationRules&lt;ProductValidation&gt;</c>)</item>
/// <item>Presentation Layer: Named Context 사용 (<c>ValidationRules.For("...")</c>)</item>
/// </list>
/// </para>
/// </remarks>
public static class ValidationRules
{
    /// <summary>
    /// Named Context 검증 컨텍스트를 생성합니다.
    /// </summary>
    /// <param name="contextName">컨텍스트 이름</param>
    /// <returns>검증 컨텍스트</returns>
    /// <example>
    /// <code>
    /// // 단일 검증
    /// var result = ValidationRules.For("ProductName").NotEmpty(name);
    ///
    /// // 체이닝
    /// var result = ValidationRules.For("Price")
    ///     .Positive(amount)
    ///     .ThenAtMost(1000000m);
    /// </code>
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValidationContext For(string contextName) => new(contextName);
}
