namespace Framework.Abstractions.Errors;

/// <summary>
/// 모든 레이어에서 공통으로 사용할 수 있는 에러 타입의 기반 클래스
/// </summary>
/// <remarks>
/// Domain, Application, Adapter 레이어의 에러 타입이 이 클래스를 상속합니다.
/// 각 레이어별 특화 에러 타입은 파생 클래스에서 정의합니다.
///
/// 참고: C#의 nested sealed record는 파생 클래스로 자동 상속되지 않으므로,
/// 공통 에러 타입(Empty, NotFound 등)은 각 파생 클래스에서 별도로 정의해야 합니다.
/// </remarks>
public abstract record ErrorType
{
    #region 레이어별 에러 코드 접두사 상수

    /// <summary>
    /// 도메인 레이어 에러 코드 접두사
    /// </summary>
    public const string DomainErrorsPrefix = "DomainErrors";

    /// <summary>
    /// 애플리케이션 레이어 에러 코드 접두사
    /// </summary>
    public const string ApplicationErrorsPrefix = "ApplicationErrors";

    /// <summary>
    /// 어댑터 레이어 에러 코드 접두사
    /// </summary>
    public const string AdapterErrorsPrefix = "AdapterErrors";

    #endregion

    /// <summary>
    /// 에러 코드에 사용될 에러 이름
    /// 기본적으로 record 타입 이름을 반환
    /// </summary>
    public virtual string ErrorName => GetType().Name;
}
