using LanguageExt.Common;

namespace Functorium.Applications.Usecases;

// =============================================================================
// IFinResponse 파일 구조
// =============================================================================
// IFinResponse.cs               - 인터페이스 정의 (IFinResponse, IFinResponseFactory 등)
// IFinResponse.Impl.cs          - FinResponse<A> 레코드 (Succ/Fail 중첩 타입, 메서드, 연산자)
// IFinResponse.Factory.cs       - FinResponse 정적 팩토리 클래스
// IFinResponse.FinConversions.cs - Fin<A> → FinResponse<A> 변환 확장 메서드
// =============================================================================

/// <summary>
/// FinResponse 타입의 기본 인터페이스 (제네릭 없음).
/// Pipeline에서 IsSucc/IsFail 속성에 접근하기 위해 사용됩니다.
/// </summary>
public interface IFinResponse
{
    /// <summary>
    /// Is the structure in a Success state?
    /// </summary>
    bool IsSucc { get; }

    /// <summary>
    /// Is the structure in a Fail state?
    /// </summary>
    bool IsFail { get; }
}

/// <summary>
/// FinResponse 타입의 제네릭 인터페이스 (공변성 지원).
/// Pipeline에서 읽기 전용으로 사용됩니다.
/// </summary>
public interface IFinResponse<out A> : IFinResponse
{
}

/// <summary>
/// Pipeline에서 Error 정보에 접근하기 위한 인터페이스.
/// Logger, Trace Pipeline에서 사용됩니다.
/// </summary>
public interface IFinResponseWithError
{
    /// <summary>
    /// 실패 시 Error 정보
    /// </summary>
    Error Error { get; }
}

/// <summary>
/// FinResponse 생성을 위한 제네릭 인터페이스.
/// CRTP(Curiously Recurring Template Pattern)를 사용하여 타입 안전한 Fail 생성을 지원합니다.
/// </summary>
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    /// <summary>
    /// 실패 FinResponse를 생성합니다.
    /// </summary>
    static abstract TSelf CreateFail(Error error);
}
