using System.Diagnostics.Contracts;
using LanguageExt.Common;

namespace Functorium.Applications.Cqrs;

/// <summary>
/// FinResponse 타입의 기본 인터페이스 (제네릭 없음).
/// Pipeline에서 IsSucc/IsFail 속성에 접근하기 위해 사용됩니다.
/// </summary>
public interface IFinResponse
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
/// FinResponse 타입의 제네릭 인터페이스 (공변성 지원).
/// Pipeline에서 읽기 전용으로 사용됩니다.
/// </summary>
public interface IFinResponse<out A> : IFinResponse
{
}

/// <summary>
/// Pipeline에서 Error 정보에 접근하기 위한 인터페이스.
/// Logger, Trace Pipeline에서 사용됩니다.
/// </summary>
public interface IFinResponseWithError
{
    /// <summary>
    /// 실패 시 Error 정보
    /// </summary>
    Error Error { get; }
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
    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError
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

        Error IFinResponseWithError.Error => Error;
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


public static class FinResponse
{
    /// <summary>
    /// Construct a FinResponse in a Succ state
    /// </summary>
    [Pure]
    public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);

    [Pure]
    public static FinResponse<A> Succ<A>() where A : new() => new FinResponse<A>.Succ(new A());


    /// <summary>
    /// Construct a FinResponse in a Fail state
    /// </summary>
    [Pure]
    public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);
}

/// <summary>
/// FinToFinResponse{B}에서 Fin{A}로 변환하는 확장 메서드.
/// Repository(Fin) → Usecase(FinResponse) 계층 간 변환에 사용됩니다.
///
///  1. **UsecaseValidationPipeline (65줄)**과 **UsecaseExceptionPipeline (31줄)**에서 TResponse.CreateFail(error)를 호출합니다.
///  2. 이것은 IFinResponseFactory<TSelf>의 static abstract 메서드입니다:
///     public interface IFinResponseFactory<TSelf>
///     {
///         static abstract TSelf CreateFail(Error error);
///     }
///  3. static abstract 메서드는 인터페이스 타입에서 호출할 수 없습니다. 구체 타입이어야만 호출 가능합니다.
///
///  만약 전체를 IFinResponse<A>로 변경하면:
///    - ICommandRequest<TSuccess>가 ICommand<IFinResponse<TSuccess>>를 상속
///    - Pipeline의 TResponse가 IFinResponse<TSuccess>가 됨
///    - IFinResponse는 인터페이스이므로 IFinResponseFactory를 구현하지 않음
///    - TResponse.CreateFail() 호출 불가 → 컴파일 에러
/// </summary>
public static class FinToFinResponse
{
    /// <summary>
    /// Fin{A}를 FinResponse{A}로 변환합니다.
    /// </summary>
    /// <typeparam name="A">성공 값 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <returns>변환된 FinResponse{A}</returns>
    public static FinResponse<A> ToFinResponse<A>(this Fin<A> fin) =>
        fin.Match(
            Succ: FinResponse.Succ,
            Fail: FinResponse.Fail<A>);

    /// <summary>
    /// Fin{A}를 FinResponse{B}로 변환하며 성공 값을 매핑합니다.
    /// </summary>
    /// <typeparam name="A">원본 성공 값 타입</typeparam>
    /// <typeparam name="B">대상 성공 값 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <param name="mapper">성공 값을 변환하는 함수</param>
    /// <returns>변환된 FinResponse{B}</returns>
    public static FinResponse<B> ToFinResponse<A, B>(
        this Fin<A> fin,
        Func<A, B> mapper) =>
        fin.Match(
            Succ: value => FinResponse.Succ(mapper(value)),
            Fail: FinResponse.Fail<B>);

    /// <summary>
    /// Fin{A}를 FinResponse{B}로 변환하며 성공 시 factory로 인스턴스를 생성합니다.
    /// 성공 값(A)이 필요 없고 단순히 새로운 B 인스턴스 생성만 필요한 경우 사용합니다.
    /// </summary>
    /// <typeparam name="A">원본 성공 값 타입 (무시됨)</typeparam>
    /// <typeparam name="B">대상 성공 값 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <param name="factory">성공 시 B 인스턴스를 생성하는 함수</param>
    /// <returns>변환된 FinResponse{B}</returns>
    /// <example>
    /// <code>
    /// Fin{Unit} result = await repository.DeleteAsync(id);
    /// return result.ToFinResponse(() => new DeleteResponse(id));
    /// </code>
    /// </example>
    public static FinResponse<B> ToFinResponse<A, B>(
        this Fin<A> fin,
        Func<B> factory) =>
        fin.Match(
            Succ: _ => FinResponse.Succ(factory()),
            Fail: FinResponse.Fail<B>);

    /// <summary>
    /// Fin{A}를 FinResponse{B}로 변환합니다.
    /// 성공/실패 모두에 대해 커스텀 처리가 필요한 경우 사용합니다.
    /// </summary>
    /// <typeparam name="A">원본 성공 값 타입</typeparam>
    /// <typeparam name="B">대상 성공 값 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <param name="onSucc">성공 시 FinResponse를 생성하는 함수</param>
    /// <param name="onFail">실패 시 FinResponse를 생성하는 함수</param>
    /// <returns>변환된 FinResponse{B}</returns>
    public static FinResponse<B> ToFinResponse<A, B>(
        this Fin<A> fin,
        Func<A, FinResponse<B>> onSucc,
        Func<Error, FinResponse<B>> onFail) =>
        fin.Match(
            Succ: onSucc,
            Fail: onFail);
}
