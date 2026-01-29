namespace Functorium.Domains.ValueObjects.Validations.Contextual;

/// <summary>
/// ContextualValidation 체이닝을 위한 확장 메서드
/// </summary>
/// <remarks>
/// <para>
/// Named Context 검증 패턴에서 체이닝을 지원합니다.
/// 컨텍스트 이름이 자동으로 전파됩니다.
/// </para>
/// <para>
/// <b>사용 예시:</b>
/// <code>
/// ValidationRules.For("ProductName")
///     .NotEmpty(name)
///     .ThenMinLength(3)
///     .ThenMaxLength(100);
/// </code>
/// </para>
/// </remarks>
public static partial class ContextualValidationExtensions;
