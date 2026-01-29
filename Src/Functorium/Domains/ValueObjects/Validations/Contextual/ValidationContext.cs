namespace Functorium.Domains.ValueObjects.Validations.Contextual;

/// <summary>
/// Named Context 검증을 위한 컨텍스트 구조체
/// </summary>
/// <remarks>
/// <para>
/// Value Object 없이 primitive 타입을 검증할 때 사용합니다.
/// DTO 검증, API 입력 검증, 빠른 프로토타이핑에 적합합니다.
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
/// <b>참고:</b> 도메인 레이어에서는 Value Object를 사용하는 것이 권장됩니다.
/// </para>
/// </remarks>
public readonly partial struct ValidationContext
{
    /// <summary>
    /// 검증 컨텍스트 이름
    /// </summary>
    public string ContextName { get; }

    /// <summary>
    /// ValidationContext 생성
    /// </summary>
    /// <param name="contextName">컨텍스트 이름</param>
    public ValidationContext(string contextName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);
        ContextName = contextName;
    }
}
