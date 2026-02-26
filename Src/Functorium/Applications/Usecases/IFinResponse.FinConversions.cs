using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Usecases;

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
