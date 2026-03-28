using System.Diagnostics.Contracts;
using LanguageExt.Common;

namespace ResultPattern.Demo.Cqrs;

/// <summary>
/// FinResponse 타입의 기본 인터페이스 (공변성 지원).
/// Pipeline에서 읽기 전용으로 사용됩니다.
/// </summary>
public interface IFinResponse<out A>
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

/// <summary>
/// Equivalent of `Either〈Error, A〉` - LanguageExt Fin{A} 스타일.
/// 기본 생성자 없이 Response를 정의할 수 있도록 지원합니다.
/// </summary>
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
{
    /// <summary>
    /// Success case
    /// </summary>
    public sealed record Succ(A Value) : FinResponse<A>
    {
        [Pure]
        public override bool IsSucc => true;

        [Pure]
        public override bool IsFail => false;

        [Pure]
        public override B Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => Succ(Value);

        [Pure]
        public override string ToString() => Value is null ? "Succ(null)" : $"Succ({Value})";

        public void Deconstruct(out A value) => value = Value;

        internal override A SuccValue => Value;
        internal override Error FailValue => throw new InvalidOperationException("Cannot access FailValue on Succ");
    }

    /// <summary>
    /// Fail case
    /// </summary>
    public sealed record Fail(Error Error) : FinResponse<A>
    {
        [Pure]
        public override bool IsSucc => false;

        [Pure]
        public override bool IsFail => true;

        [Pure]
        public override B Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => Fail(Error);

        [Pure]
        public override string ToString() => $"Fail({Error})";

        public void Deconstruct(out Error error) => error = Error;

        internal override A SuccValue => throw new InvalidOperationException("Cannot access SuccValue on Fail");
        internal override Error FailValue => Error;
    }

    /// <summary>
    /// Is the structure in a Success state?
    /// </summary>
    [Pure]
    public abstract bool IsSucc { get; }

    /// <summary>
    /// Is the structure in a Fail state?
    /// </summary>
    [Pure]
    public abstract bool IsFail { get; }

    /// <summary>
    /// Unsafe access to the success value
    /// </summary>
    internal abstract A SuccValue { get; }

    /// <summary>
    /// Unsafe access to the fail value
    /// </summary>
    internal abstract Error FailValue { get; }

    /// <summary>
    /// Invokes the Succ or Fail function depending on the state of the structure
    /// </summary>
    [Pure]
    public abstract B Match<B>(Func<A, B> Succ, Func<Error, B> Fail);

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

    /// <summary>
    /// Maps the success value
    /// </summary>
    [Pure]
    public FinResponse<B> Map<B>(Func<A, B> f) =>
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

    /// <summary>
    /// Monadic bind
    /// </summary>
    [Pure]
    public FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f) =>
        Match(
            Succ: f,
            Fail: FinResponse.Fail<B>);

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

    /// <summary>
    /// LINQ Select support
    /// </summary>
    [Pure]
    public FinResponse<B> Select<B>(Func<A, B> f) => Map(f);

    /// <summary>
    /// LINQ SelectMany support
    /// </summary>
    [Pure]
    public FinResponse<C> SelectMany<B, C>(Func<A, FinResponse<B>> bind, Func<A, B, C> project) =>
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

    #region Factory Methods

    /// <summary>
    /// IFinResponseFactory.CreateFail implementation (Pipeline용)
    /// </summary>
    public static FinResponse<A> CreateFail(Error error) => new Fail(error);

    #endregion

    #region Operators

    [Pure]
    public static implicit operator FinResponse<A>(A value) => new Succ(value);

    [Pure]
    public static implicit operator FinResponse<A>(Error error) => new Fail(error);

    [Pure]
    public static bool operator true(FinResponse<A> ma) => ma.IsSucc;

    [Pure]
    public static bool operator false(FinResponse<A> ma) => ma.IsFail;

    /// <summary>
    /// Choice operator - returns lhs if Succ, otherwise rhs
    /// </summary>
    [Pure]
    public static FinResponse<A> operator |(FinResponse<A> lhs, FinResponse<A> rhs) =>
        lhs.IsSucc ? lhs : rhs;

    #endregion
}

/// <summary>
/// Static helper class for FinResponse creation - LanguageExt Fin style
/// </summary>
public static class FinResponse
{
    /// <summary>
    /// Construct a FinResponse in a Succ state
    /// </summary>
    [Pure]
    public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);

    /// <summary>
    /// Construct a FinResponse in a Fail state
    /// </summary>
    [Pure]
    public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);

    /// <summary>
    /// Construct a FinResponse in a Fail state from string
    /// </summary>
    [Pure]
    public static FinResponse<A> Fail<A>(string error) => new FinResponse<A>.Fail(Error.New(error));
}
