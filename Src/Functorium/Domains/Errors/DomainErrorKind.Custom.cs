namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorKind
{
    /// <summary>
    /// 도메인 특화 커스텀 에러의 기본 클래스 (표준 에러에 해당하지 않는 경우)
    /// </summary>
    /// <remarks>
    /// 표준 에러로 표현할 수 없는 도메인 특화 에러에 사용합니다.
    /// 파생 sealed record로 정의하여 타입 안전하게 사용합니다.
    /// <code>
    /// // 엔티티 내부에 nested record로 정의
    /// public sealed record InsufficientStock : DomainErrorKind.Custom;
    ///
    /// DomainError.For&lt;Inventory&gt;(new InsufficientStock(), currentStock, "Insufficient stock");
    /// </code>
    /// </remarks>
    public abstract record Custom : DomainErrorKind;
}
