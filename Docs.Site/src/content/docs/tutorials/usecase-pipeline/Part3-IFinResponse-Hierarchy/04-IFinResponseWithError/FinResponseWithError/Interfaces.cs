using LanguageExt.Common;

namespace FinResponseWithError;

public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}

/// <summary>
/// 에러 접근 인터페이스.
/// Fail 케이스에서만 구현하여 타입 안전하게 에러에 접근합니다.
/// </summary>
public interface IFinResponseWithError
{
    Error Error { get; }
}
