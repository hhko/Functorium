using LanguageExt.Common;

namespace FinResponseCovariant;

public record CovariantResponse<A> : IFinResponse<A>
{
    private readonly A? _value;
    private readonly Error? _error;

    private CovariantResponse(A value) { _value = value; _error = null; }
    private CovariantResponse(Error error) { _value = default; _error = error; }

    public bool IsSucc => _error is null;
    public bool IsFail => _error is not null;

    public A Value => IsSucc ? _value! : throw new InvalidOperationException("Cannot access Value on Fail");

    public static CovariantResponse<A> Succ(A value) => new(value);
    public static CovariantResponse<A> Fail(Error error) => new(error);
}
