using System.Diagnostics.Contracts;
using LanguageExt.Common;

namespace FinResponseDiscriminatedUnion;

/// <summary>
/// 모든 인터페이스를 통합하는 FinResponse<A> Discriminated Union.
/// </summary>
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
{
    public sealed record Succ(A Value) : FinResponse<A>
    {
        [Pure] public override bool IsSucc => true;
        [Pure] public override bool IsFail => false;

        [Pure]
        public override B Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => Succ(Value);

        [Pure]
        public override string ToString() => Value is null ? "Succ(null)" : $"Succ({Value})";

        public void Deconstruct(out A value) => value = Value;

        internal override A SuccValue => Value;
        internal override Error FailValue => throw new InvalidOperationException("Cannot access FailValue on Succ");
    }

    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError
    {
        [Pure] public override bool IsSucc => false;
        [Pure] public override bool IsFail => true;

        [Pure]
        public override B Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => Fail(Error);

        [Pure]
        public override string ToString() => $"Fail({Error})";

        public void Deconstruct(out Error error) => error = Error;

        internal override A SuccValue => throw new InvalidOperationException("Cannot access SuccValue on Fail");
        internal override Error FailValue => Error;

        Error IFinResponseWithError.Error => Error;
    }

    [Pure] public abstract bool IsSucc { get; }
    [Pure] public abstract bool IsFail { get; }

    internal abstract A SuccValue { get; }
    internal abstract Error FailValue { get; }

    [Pure] public abstract B Match<B>(Func<A, B> Succ, Func<Error, B> Fail);

    /// <summary>
    /// Invokes the Succ or Fail action depending on the state of the structure
    /// </summary>
    public void Match(Action<A> Succ, Action<Error> Fail)
    {
        if (IsSucc)
            Succ(SuccValue);
        else
            Fail(FailValue);
    }

    /// <summary>
    /// Returns the success value or invokes the Fail function to get an alternative
    /// </summary>
    [Pure]
    public A IfFail(Func<Error, A> Fail) => Match(x => x, Fail);

    /// <summary>
    /// Returns the success value or the alternative value
    /// </summary>
    [Pure]
    public A IfFail(A alternative) => Match(x => x, _ => alternative);

    /// <summary>
    /// Invokes the Fail action if in a Fail state
    /// </summary>
    public void IfFail(Action<Error> Fail)
    {
        if (IsFail) Fail(FailValue);
    }

    /// <summary>
    /// Invokes the Succ action if in a Succ state
    /// </summary>
    public void IfSucc(Action<A> Succ)
    {
        if (IsSucc) Succ(SuccValue);
    }

    [Pure] public FinResponse<B> Map<B>(Func<A, B> f) =>
        Match(
            Succ: value => FinResponse.Succ(f(value)),
            Fail: FinResponse.Fail<B>);

    /// <summary>
    /// Maps the fail value
    /// </summary>
    [Pure]
    public FinResponse<A> MapFail(Func<Error, Error> f) =>
        Match(
            Succ: FinResponse.Succ,
            Fail: error => FinResponse.Fail<A>(f(error)));

    /// <summary>
    /// Bi-maps the structure
    /// </summary>
    [Pure]
    public FinResponse<B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail) =>
        Match(
            Succ: value => FinResponse.Succ(Succ(value)),
            Fail: error => FinResponse.Fail<B>(Fail(error)));

    [Pure] public FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f) =>
        Match(Succ: f, Fail: FinResponse.Fail<B>);

    /// <summary>
    /// Bi-bind. Allows mapping of both monad states
    /// </summary>
    [Pure]
    public FinResponse<B> BiBind<B>(Func<A, FinResponse<B>> Succ, Func<Error, FinResponse<B>> Fail) =>
        Match(Succ, Fail);

    /// <summary>
    /// Bind if in a fail state
    /// </summary>
    [Pure]
    public FinResponse<A> BindFail(Func<Error, FinResponse<A>> Fail) =>
        BiBind(FinResponse.Succ, Fail);

    // LINQ support
    [Pure] public FinResponse<B> Select<B>(Func<A, B> f) => Map(f);

    [Pure] public FinResponse<C> SelectMany<B, C>(
        Func<A, FinResponse<B>> bind, Func<A, B, C> project) =>
        Bind(a => bind(a).Map(b => project(a, b)));

    /// <summary>
    /// Throws if in a Fail state, otherwise returns the success value
    /// </summary>
    public A ThrowIfFail()
    {
        if (IsFail)
            FailValue.Throw();
        return SuccValue;
    }

    // CRTP factory
    public static FinResponse<A> CreateFail(Error error) => new Fail(error);

    #region Operators

    [Pure] public static implicit operator FinResponse<A>(A value) => new Succ(value);
    [Pure] public static implicit operator FinResponse<A>(Error error) => new Fail(error);

    [Pure] public static bool operator true(FinResponse<A> ma) => ma.IsSucc;
    [Pure] public static bool operator false(FinResponse<A> ma) => ma.IsFail;

    /// <summary>
    /// Choice operator - returns lhs if Succ, otherwise rhs
    /// </summary>
    [Pure]
    public static FinResponse<A> operator |(FinResponse<A> lhs, FinResponse<A> rhs) =>
        lhs.IsSucc ? lhs : rhs;

    #endregion
}

/// <summary>
/// 정적 팩토리
/// </summary>
public static class FinResponse
{
    [Pure] public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);
    [Pure] public static FinResponse<A> Succ<A>() where A : new() => new FinResponse<A>.Succ(new A());
    [Pure] public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);
}
