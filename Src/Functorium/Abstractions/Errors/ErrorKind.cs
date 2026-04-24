namespace Functorium.Abstractions.Errors;

/// <summary>
/// 모든 레이어에서 공통으로 사용할 수 있는 에러 타입의 기반 클래스
/// </summary>
/// <remarks>
/// Domain, Application, Adapter 레이어의 에러 타입이 이 클래스를 상속합니다.
/// 각 레이어별 특화 에러 타입은 파생 클래스에서 정의합니다.
///
/// 참고: C#의 nested sealed record는 파생 클래스로 자동 상속되지 않으므로,
/// 공통 에러 타입(Empty, NotFound 등)은 각 파생 클래스에서 별도로 정의해야 합니다.
///
/// 레이어 접두사 상수는 <see cref="ErrorCodePrefixes"/>에 internal로
/// 정의되어 있으며, 레이어 팩토리가 내부적으로 사용합니다.
/// </remarks>
public abstract record ErrorKind
{
    /// <summary>
    /// 에러 코드에 사용될 에러 이름
    /// 기본적으로 record 타입 이름을 반환
    /// </summary>
    public virtual string Name => GetType().Name;
}
