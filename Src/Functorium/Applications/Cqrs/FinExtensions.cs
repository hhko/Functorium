using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Cqrs;

/// <summary>
/// Fin{T} 타입에 대한 IResponse 변환 확장 메서드
/// </summary>
public static class FinExtensions
{
    /// <summary>
    /// Fin{TSource}를 IResponse{TResponse}로 변환합니다.
    /// 성공 시 mapper를 통해 TResponse를 생성하고,
    /// 실패 시 TResponse.CreateFail(error)를 호출합니다.
    /// </summary>
    /// <typeparam name="TSource">원본 성공 값 타입</typeparam>
    /// <typeparam name="TResponse">대상 Response 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <param name="mapper">성공 값을 Response로 변환하는 함수</param>
    /// <returns>변환된 TResponse</returns>
    public static TResponse ToResponse<TSource, TResponse>(
        this Fin<TSource> fin,
        Func<TSource, TResponse> mapper)
        where TResponse : IResponse<TResponse>
    {
        return fin.Match(
            Succ: mapper,
            Fail: error => TResponse.CreateFail(error));
    }

    /// <summary>
    /// Fin{TSource}를 IResponse{TResponse}로 변환합니다.
    /// 성공/실패 모두에 대해 커스텀 처리가 필요한 경우 사용합니다.
    /// </summary>
    /// <typeparam name="TSource">원본 성공 값 타입</typeparam>
    /// <typeparam name="TResponse">대상 Response 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <param name="onSuccess">성공 시 Response를 생성하는 함수</param>
    /// <param name="onFail">실패 시 Response를 생성하는 함수</param>
    /// <returns>변환된 TResponse</returns>
    public static TResponse ToResponse<TSource, TResponse>(
        this Fin<TSource> fin,
        Func<TSource, TResponse> onSuccess,
        Func<Error, TResponse> onFail)
        where TResponse : IResponse<TResponse>
    {
        return fin.Match(
            Succ: onSuccess,
            Fail: onFail);
    }

    /// <summary>
    /// Fin{TSource}가 성공인 경우 mapper로 TResponse를 생성합니다.
    /// 실패인 경우 null을 반환합니다.
    /// 성공 케이스만 처리하고 실패는 상위에서 별도 처리할 때 사용합니다.
    /// </summary>
    /// <typeparam name="TSource">원본 성공 값 타입</typeparam>
    /// <typeparam name="TResponse">대상 Response 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <param name="mapper">성공 값을 Response로 변환하는 함수</param>
    /// <returns>성공 시 변환된 TResponse, 실패 시 null</returns>
    public static TResponse? ToResponseOrNull<TSource, TResponse>(
        this Fin<TSource> fin,
        Func<TSource, TResponse> mapper)
        where TResponse : class, IResponse<TResponse>
    {
        return fin.Match<TResponse?>(
            Succ: mapper,
            Fail: _ => null);
    }

    /// <summary>
    /// Fin{TSource}가 실패인 경우 TResponse.CreateFail(error)를 반환합니다.
    /// 성공인 경우 null을 반환합니다.
    /// 실패 케이스만 먼저 처리하고 성공은 이후 로직에서 처리할 때 사용합니다.
    /// </summary>
    /// <typeparam name="TSource">원본 성공 값 타입</typeparam>
    /// <typeparam name="TResponse">대상 Response 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <returns>실패 시 CreateFail Response, 성공 시 null</returns>
    public static TResponse? ToFailResponseOrNull<TSource, TResponse>(
        this Fin<TSource> fin)
        where TResponse : class, IResponse<TResponse>
    {
        return fin.Match<TResponse?>(
            Succ: _ => null,
            Fail: error => TResponse.CreateFail(error));
    }
}
