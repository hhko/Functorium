using LanguageExt.Common;

namespace FinResponseDiscriminatedUnion;

public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}

public interface IFinResponse<out A> : IFinResponse;

public interface IFinResponseWithError
{
    Error Error { get; }
}

public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
