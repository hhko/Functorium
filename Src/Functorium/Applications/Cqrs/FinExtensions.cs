using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Cqrs;

/// <summary>
/// Fin{T} 타입에 대한 IResponse 변환 확장 메서드
/// </summary>
public static class FinExtensions
{
    /// <summary>
    /// Fin{T}를 T로 변환합니다.
    /// 성공 시 값을 그대로 반환하고,
    /// 실패 시 T.CreateFail(error)를 호출합니다.
    /// </summary>
    /// <typeparam name="T">IResponse{T}를 구현하는 타입</typeparam>
    /// <param name="fin">변환할 Fin 인스턴스</param>
    /// <returns>변환된 T</returns>
    public static T ToResponse<T>(this Fin<T> fin)
        where T : IResponse<T>
    {
        return fin.Match(
            Succ: value => value,
            Fail: error => T.CreateFail(error));
    }

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
}
