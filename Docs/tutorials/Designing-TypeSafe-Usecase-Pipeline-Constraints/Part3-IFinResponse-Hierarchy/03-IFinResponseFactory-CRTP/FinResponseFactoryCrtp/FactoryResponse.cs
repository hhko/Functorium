using LanguageExt.Common;

namespace FinResponseFactoryCrtp;

/// <summary>
/// IFinResponseFactory를 구현하는 응답 타입.
/// CreateFail을 static abstract로 구현합니다.
/// </summary>
public record FactoryResponse<A> : IFinResponseFactory<FactoryResponse<A>>
{
    private readonly A? _value;
    private readonly Error? _error;

    private FactoryResponse(A value) { _value = value; _error = null; }
    private FactoryResponse(Error error) { _value = default; _error = error; }

    public bool IsSucc => _error is null;
    public bool IsFail => _error is not null;
    public A Value => IsSucc ? _value! : throw new InvalidOperationException("Cannot access Value on Fail");

    public static FactoryResponse<A> Succ(A value) => new(value);

    // CRTP: static abstract 구현
    public static FactoryResponse<A> CreateFail(Error error) => new(error);
}
