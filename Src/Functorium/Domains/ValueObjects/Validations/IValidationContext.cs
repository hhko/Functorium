namespace Functorium.Domains.ValueObjects.Validations;

/// <summary>
/// 검증 컨텍스트를 나타내는 마커 인터페이스
/// </summary>
/// <remarks>
/// <para>
/// Application Layer나 Presentation Layer에서 Value Object 없이
/// primitive 타입을 검증할 때 사용하는 컨텍스트 클래스를 식별합니다.
/// </para>
/// <para>
/// <b>사용 예시:</b>
/// <code>
/// // Application Layer에서 재사용 가능한 검증 컨텍스트
/// public sealed class ProductValidation : IValidationContext;
///
/// // 사용
/// ValidationRules&lt;ProductValidation&gt;.Positive(amount);
/// // Error: DomainErrors.ProductValidation.NotPositive
/// </code>
/// </para>
/// <para>
/// <b>참고:</b> 도메인 레이어에서는 Value Object를 직접 사용하는 것이 권장됩니다.
/// <code>
/// ValidationRules&lt;Price&gt;.Positive(amount);
/// </code>
/// </para>
/// </remarks>
public interface IValidationContext;
