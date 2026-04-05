using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

namespace Functorium.Adapters.Errors;

/// <summary>
/// 어댑터의 오류 생성을 위한 헬퍼 클래스
/// 에러 코드를 자동으로 "AdapterErrors.{AdapterName}.{ErrorName}" 형식으로 생성
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Adapters.Errors.AdapterErrorType;
///
/// AdapterError.For&lt;UsecaseValidationPipeline&gt;(new PipelineValidation("PropertyName"), value, "Validation failed");
/// AdapterError.FromException&lt;UsecaseExceptionPipeline&gt;(new PipelineException(), exception);
/// // 커스텀 에러: sealed record 파생 정의
/// // public sealed record RateLimited : AdapterErrorType.Custom;
/// AdapterError.For&lt;HttpClientAdapter&gt;(new RateLimited(), url, "Rate limit exceeded");
/// </code>
/// </remarks>
public static class AdapterError
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter>(
        AdapterErrorType errorType,
        string currentValue,
        string message) =>
        LayerErrorCore.Create<TAdapter>(ErrorType.AdapterErrorsPrefix, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For(
        Type adapterType,
        AdapterErrorType errorType,
        string currentValue,
        string message) =>
        LayerErrorCore.Create(ErrorType.AdapterErrorsPrefix, adapterType, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter, TValue>(
        AdapterErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        LayerErrorCore.Create<TAdapter, TValue>(ErrorType.AdapterErrorsPrefix, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter, T1, T2>(
        AdapterErrorType errorType,
        T1 value1,
        T2 value2,
        string message)
        where T1 : notnull
        where T2 : notnull =>
        LayerErrorCore.Create<TAdapter, T1, T2>(ErrorType.AdapterErrorsPrefix, errorType, value1, value2, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter, T1, T2, T3>(
        AdapterErrorType errorType,
        T1 value1,
        T2 value2,
        T3 value3,
        string message)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        LayerErrorCore.Create<TAdapter, T1, T2, T3>(ErrorType.AdapterErrorsPrefix, errorType, value1, value2, value3, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error FromException<TAdapter>(
        AdapterErrorType errorType,
        Exception exception) =>
        LayerErrorCore.FromException<TAdapter>(ErrorType.AdapterErrorsPrefix, errorType, exception);
}
