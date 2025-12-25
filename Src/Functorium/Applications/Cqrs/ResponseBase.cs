using LanguageExt.Common;

namespace Functorium.Applications.Cqrs;

public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>, new()
{
    public bool IsSuccess { get; init; } = true;
    public Error? Error { get; init; } = null;

    public static TSelf CreateFail(Error error)
        => new TSelf() { IsSuccess = false, Error = error };
}
