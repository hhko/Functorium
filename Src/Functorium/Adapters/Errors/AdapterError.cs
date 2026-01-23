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
/// AdapterError.For&lt;HttpClientAdapter&gt;(new Custom("RateLimited"), url, "Rate limit exceeded");
/// </code>
/// </remarks>
public static class AdapterError
{
    /// <summary>
    /// AdapterErrorType record를 사용하여 에러를 생성합니다.
    /// </summary>
    /// <typeparam name="TAdapter">어댑터 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter>(
        AdapterErrorType errorType,
        string currentValue,
        string message) =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.AdapterErrorsPrefix}.{typeof(TAdapter).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// AdapterErrorType record를 사용하여 에러를 생성합니다. (제네릭 값 타입)
    /// </summary>
    /// <typeparam name="TAdapter">어댑터 타입</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter, TValue>(
        AdapterErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.AdapterErrorsPrefix}.{typeof(TAdapter).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// AdapterErrorType record를 사용하여 에러를 생성합니다. (두 개의 값 포함)
    /// </summary>
    /// <typeparam name="TAdapter">어댑터 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="value1">첫 번째 값</param>
    /// <param name="value2">두 번째 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TAdapter, T1, T2>(
        AdapterErrorType errorType,
        T1 value1,
        T2 value2,
        string message)
        where T1 : notnull
        where T2 : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.AdapterErrorsPrefix}.{typeof(TAdapter).Name}.{errorType.ErrorName}",
            value1,
            value2,
            errorMessage: message);

    /// <summary>
    /// AdapterErrorType record를 사용하여 에러를 생성합니다. (세 개의 값 포함)
    /// </summary>
    /// <typeparam name="TAdapter">어댑터 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <typeparam name="T3">세 번째 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="value1">첫 번째 값</param>
    /// <param name="value2">두 번째 값</param>
    /// <param name="value3">세 번째 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
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
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.AdapterErrorsPrefix}.{typeof(TAdapter).Name}.{errorType.ErrorName}",
            value1,
            value2,
            value3,
            errorMessage: message);

    /// <summary>
    /// 예외를 AdapterError로 래핑합니다.
    /// </summary>
    /// <typeparam name="TAdapter">어댑터 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="exception">래핑할 예외</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error FromException<TAdapter>(
        AdapterErrorType errorType,
        Exception exception) =>
        ErrorCodeFactory.CreateFromException(
            errorCode: $"{ErrorType.AdapterErrorsPrefix}.{typeof(TAdapter).Name}.{errorType.ErrorName}",
            exception: exception);
}
