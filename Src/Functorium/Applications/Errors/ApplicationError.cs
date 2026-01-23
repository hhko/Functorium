using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

namespace Functorium.Applications.Errors;

/// <summary>
/// 유스케이스의 애플리케이션 오류 생성을 위한 헬퍼 클래스
/// 에러 코드를 자동으로 "ApplicationErrors.{UsecaseName}.{ErrorName}" 형식으로 생성
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Applications.Errors.ApplicationErrorType;
///
/// ApplicationError.For&lt;CreateProductCommand&gt;(new AlreadyExists(), productId, "Product already exists");
/// ApplicationError.For&lt;UpdateOrderCommand&gt;(new ValidationFailed("Quantity"), value, "Quantity must be positive");
/// ApplicationError.For&lt;DeleteOrderCommand&gt;(new Custom("CannotDeleteShipped"), orderId, "Cannot delete shipped order");
/// </code>
/// </remarks>
public static class ApplicationError
{
    /// <summary>
    /// ApplicationErrorType record를 사용하여 에러를 생성합니다.
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase>(
        ApplicationErrorType errorType,
        string currentValue,
        string message) =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// ApplicationErrorType record를 사용하여 에러를 생성합니다. (제네릭 값 타입)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase, TValue>(
        ApplicationErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// ApplicationErrorType record를 사용하여 에러를 생성합니다. (두 개의 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="value1">첫 번째 값</param>
    /// <param name="value2">두 번째 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase, T1, T2>(
        ApplicationErrorType errorType,
        T1 value1,
        T2 value2,
        string message)
        where T1 : notnull
        where T2 : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{errorType.ErrorName}",
            value1,
            value2,
            errorMessage: message);

    /// <summary>
    /// ApplicationErrorType record를 사용하여 에러를 생성합니다. (세 개의 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
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
    public static Error For<TUsecase, T1, T2, T3>(
        ApplicationErrorType errorType,
        T1 value1,
        T2 value2,
        T3 value3,
        string message)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{errorType.ErrorName}",
            value1,
            value2,
            value3,
            errorMessage: message);
}
