using LanguageExt.Common;

namespace FinResponseWrapperLimitation;

/// <summary>
/// 래퍼 인터페이스로 Fin<T>의 상태를 노출합니다.
/// Pipeline에서 is IFinResponseWrapper로 캐스팅하여 접근할 수 있지만,
/// CreateFail은 여전히 해결되지 않습니다.
/// </summary>
public interface IFinResponseWrapper
{
    bool IsSucc { get; }
    bool IsFail { get; }
    Error GetError();
}

/// <summary>
/// 비즈니스 응답 인터페이스 (첫 번째 인터페이스)
/// </summary>
public interface IResponse;

/// <summary>
/// Fin<T>를 감싸는 래퍼 (두 번째 인터페이스)
/// 이중 인터페이스 문제: IResponse + IFinResponseWrapper 두 개가 필요
/// </summary>
public record ResponseWrapper<T>(T? Value, Error? Error) : IResponse, IFinResponseWrapper
    where T : IResponse
{
    public bool IsSucc => Error is null;
    public bool IsFail => Error is not null;
    public Error GetError() => Error ?? Error.New("No error");

    public static ResponseWrapper<T> Success(T value) => new(value, null);
    public static ResponseWrapper<T> Fail(Error error) => new(default, error);
}

// Pipeline에서의 문제점
public static class WrapperPipelineExample
{
    /// <summary>
    /// Pipeline에서 래퍼를 사용하면 is 캐스팅이 필요합니다 (리플렉션 1곳).
    /// 그리고 CreateFail을 호출할 방법이 없습니다.
    /// </summary>
    public static string ProcessResponse<TResponse>(TResponse response)
    {
        // 리플렉션 1곳: is 캐스팅
        if (response is IFinResponseWrapper wrapper)
        {
            return wrapper.IsSucc ? "Success" : $"Fail: {wrapper.GetError()}";
        }
        return "Unknown";
    }

    // CreateFail은 불가능 - TResponse의 제네릭 인자를 알 수 없음
    // public static TResponse CreateFail<TResponse>(Error error) => ???
}
