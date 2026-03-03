using LanguageExt.Common;

namespace FinResponseMarker;

/// <summary>
/// IFinResponse를 구현하는 간단한 응답 타입
/// </summary>
public record SimpleResponse<T> : IFinResponse
{
    private readonly T? _value;
    private readonly Error? _error;

    private SimpleResponse(T value) { _value = value; _error = null; }
    private SimpleResponse(Error error) { _value = default; _error = error; }

    public bool IsSucc => _error is null;
    public bool IsFail => _error is not null;

    public T Value => IsSucc ? _value! : throw new InvalidOperationException("Cannot access Value on Fail");
    public Error Error => IsFail ? _error! : throw new InvalidOperationException("Cannot access Error on Succ");

    public static SimpleResponse<T> Succ(T value) => new(value);
    public static SimpleResponse<T> Fail(Error error) => new(error);
}
