using System.Reflection;
using LanguageExt;
using LanguageExt.Common;

namespace FinDirectLimitation;

/// <summary>
/// Fin<T>를 Pipeline에서 사용하려면 리플렉션이 필요합니다.
/// 이 유틸리티는 리플렉션을 3곳에서 사용합니다.
/// </summary>
public static class FinReflectionUtility
{
    /// <summary>
    /// 리플렉션 1: IsSucc 확인
    /// </summary>
    public static bool IsSucc<TResponse>(TResponse response)
    {
        var type = response!.GetType();
        var property = type.GetProperty("IsSucc");
        return property is not null && (bool)property.GetValue(response)!;
    }

    /// <summary>
    /// 리플렉션 2: Error 추출
    /// </summary>
    public static Error? GetError<TResponse>(TResponse response)
    {
        var type = response!.GetType();

        // Fin<T>.Match로 에러를 추출해야 함
        var matchMethod = type.GetMethod("Match", BindingFlags.Public | BindingFlags.Instance);
        if (matchMethod is null) return null;

        // 리플렉션으로는 제네릭 Match를 호출하기 매우 복잡
        return Error.New("Reflection-based error extraction is complex");
    }

    /// <summary>
    /// 리플렉션 3: 실패 Fin<T> 생성
    /// </summary>
    public static TResponse CreateFail<TResponse>(Error error)
    {
        // Fin<T>의 정적 팩토리를 리플렉션으로 호출해야 함
        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType) throw new InvalidOperationException();

        var innerType = responseType.GetGenericArguments()[0];
        var finType = typeof(Fin<>).MakeGenericType(innerType);
        var failMethod = finType.GetMethod("Fail", BindingFlags.Public | BindingFlags.Static);

        if (failMethod is null) throw new InvalidOperationException("Cannot find Fin<T>.Fail method");

        return (TResponse)failMethod.Invoke(null, [error])!;
    }
}
