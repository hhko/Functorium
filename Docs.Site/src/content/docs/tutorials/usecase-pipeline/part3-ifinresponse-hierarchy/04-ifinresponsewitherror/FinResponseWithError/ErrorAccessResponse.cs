using LanguageExt.Common;

namespace FinResponseWithError;

public abstract record ErrorAccessResponse<A> : IFinResponse
{
    public sealed record Succ(A Value) : ErrorAccessResponse<A>
    {
        public override bool IsSucc => true;
        public override bool IsFail => false;
    }

    /// <summary>
    /// Fail만 IFinResponseWithError를 구현
    /// </summary>
    public sealed record Fail(Error Error) : ErrorAccessResponse<A>, IFinResponseWithError
    {
        public override bool IsSucc => false;
        public override bool IsFail => true;
    }

    public abstract bool IsSucc { get; }
    public abstract bool IsFail { get; }

    public static ErrorAccessResponse<A> CreateSucc(A value) => new Succ(value);
    public static ErrorAccessResponse<A> CreateFail(Error error) => new Fail(error);
}
