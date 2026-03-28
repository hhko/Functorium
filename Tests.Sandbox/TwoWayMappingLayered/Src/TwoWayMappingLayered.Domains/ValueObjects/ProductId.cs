using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace TwoWayMappingLayered.Domains.ValueObjects;

/// <summary>
/// 상품 식별자 Value Object
/// Two-Way Mapping: Domain Core에서 기술 의존성 없이 순수한 Value Object로 정의
///
/// 구현 패턴 (가이드 참조):
/// - SimpleValueObject&lt;Guid&gt; 상속
/// - Create: CreateFromValidation 헬퍼 사용
/// - Validate: 원시 타입 반환, 타입 파라미터 한 번만 지정
/// </summary>
public sealed class ProductId : SimpleValueObject<Guid>
{
    private ProductId(Guid value) : base(value) { }

    /// <summary>
    /// 새 ProductId 생성
    /// </summary>
    public static ProductId New() => new(Guid.NewGuid());

    /// <summary>
    /// 기존 값으로 ProductId 생성 (검증 포함)
    /// </summary>
    public static Fin<ProductId> Create(Guid value) =>
        CreateFromValidation(Validate(value), v => new ProductId(v));

    /// <summary>
    /// ProductId 검증
    /// </summary>
    public static Validation<Error, Guid> Validate(Guid value) =>
        value != Guid.Empty
            ? value
            : DomainError.For<ProductId, Guid>(
                new Empty(),
                value,
                $"ProductId cannot be empty");

    /// <summary>
    /// 검증 없이 ProductId 생성 (복원 시 사용)
    /// 주의: 이미 검증된 값에만 사용해야 합니다.
    /// </summary>
    public static ProductId FromValue(Guid value) => new(value);

    /// <summary>
    /// Guid로 암시적 변환
    /// </summary>
    public static implicit operator Guid(ProductId productId) => productId.Value;
}
