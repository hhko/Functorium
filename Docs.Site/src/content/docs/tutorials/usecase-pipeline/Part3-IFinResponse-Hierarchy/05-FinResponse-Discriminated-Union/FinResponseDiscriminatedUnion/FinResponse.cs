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
        public override bool IsSucc => true;
        public override bool IsFail => false;
        public override B Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => Succ(Value);
        public override string ToString() => $"Succ({Value})";
    }

    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError
    {
        public override bool IsSucc => false;
        public override bool IsFail => true;
        public override B Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => Fail(Error);
        public override string ToString() => $"Fail({Error})";
        Error IFinResponseWithError.Error => Error;
    }

    [Pure] public abstract bool IsSucc { get; }
    [Pure] public abstract bool IsFail { get; }
    [Pure] public abstract B Match<B>(Func<A, B> Succ, Func<Error, B> Fail);

    [Pure] public FinResponse<B> Map<B>(Func<A, B> f) =>
        Match(
            Succ: value => FinResponse.Succ(f(value)),
            Fail: FinResponse.Fail<B>);

    [Pure] public FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f) =>
        Match(Succ: f, Fail: FinResponse.Fail<B>);

    // LINQ support
    [Pure] public FinResponse<B> Select<B>(Func<A, B> f) => Map(f);

    [Pure] public FinResponse<C> SelectMany<B, C>(
        Func<A, FinResponse<B>> bind, Func<A, B, C> project) =>
        Bind(a => bind(a).Map(b => project(a, b)));

    // CRTP factory
    public static FinResponse<A> CreateFail(Error error) => new Fail(error);

    // Implicit conversions
    [Pure] public static implicit operator FinResponse<A>(A value) => new Succ(value);
    [Pure] public static implicit operator FinResponse<A>(Error error) => new Fail(error);
}

/// <summary>
/// 정적 팩토리
/// </summary>
public static class FinResponse
{
    [Pure] public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);
    [Pure] public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);
}
