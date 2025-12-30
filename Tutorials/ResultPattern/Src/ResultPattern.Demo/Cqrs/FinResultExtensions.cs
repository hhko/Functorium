using LanguageExt;

namespace ResultPattern.Demo.Cqrs;

/// <summary>
/// Fin{A}를 FinResponse{A}로 변환하는 확장 메서드
/// </summary>
public static class FinResultExtensions
{
    /// <summary>
    /// Fin{A}를 FinResponse{A}로 변환
    /// </summary>
    public static FinResponse<A> ToFinResponse<A>(this Fin<A> fin)
        => fin.Match(
            Succ: FinResponse.Succ,
            Fail: FinResponse.Fail<A>);

    /// <summary>
    /// Fin{A}를 FinResponse{B}로 변환하며 성공 값을 매핑
    /// </summary>
    public static FinResponse<B> ToFinResponse<A, B>(
        this Fin<A> fin,
        Func<A, B> mapper)
        => fin.Match(
            Succ: value => FinResponse.Succ(mapper(value)),
            Fail: FinResponse.Fail<B>);
}
