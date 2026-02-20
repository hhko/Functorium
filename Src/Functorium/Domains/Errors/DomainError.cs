using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

namespace Functorium.Domains.Errors;

/// <summary>
/// 값 객체의 도메인 오류 생성을 위한 헬퍼 클래스
/// 에러 코드를 자동으로 "DomainErrors.{ValueObjectName}.{ErrorName}" 형식으로 생성
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Domains.Errors.DomainErrorType;
///
/// DomainError.For&lt;Email&gt;(new Empty(), value, "Email cannot be empty");
/// DomainError.For&lt;Password&gt;(new TooShort(MinLength: 8), value, "Password too short");
/// // 커스텀 에러: sealed record 파생 정의
/// // public sealed record Unsupported : DomainErrorType.Custom;
/// DomainError.For&lt;Currency&gt;(new Unsupported(), value, "Currency not supported");
/// </code>
/// </remarks>
public static class DomainError
{
    /// <summary>
    /// DomainErrorType record를 사용하여 에러를 생성합니다.
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TDomain>(
        DomainErrorType errorType,
        string currentValue,
        string message) =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// DomainErrorType record를 사용하여 에러를 생성합니다. (제네릭 값 타입)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TDomain, TValue>(
        DomainErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// DomainErrorType record를 사용하여 에러를 생성합니다. (두 개의 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="value1">첫 번째 값</param>
    /// <param name="value2">두 번째 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TDomain, T1, T2>(
        DomainErrorType errorType,
        T1 value1,
        T2 value2,
        string message)
        where T1 : notnull
        where T2 : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{errorType.ErrorName}",
            value1,
            value2,
            errorMessage: message);

    /// <summary>
    /// DomainErrorType record를 사용하여 에러를 생성합니다. (세 개의 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
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
    public static Error For<TDomain, T1, T2, T3>(
        DomainErrorType errorType,
        T1 value1,
        T2 value2,
        T3 value3,
        string message)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{errorType.ErrorName}",
            value1,
            value2,
            value3,
            errorMessage: message);

    /// <summary>
    /// 문자열 컨텍스트 이름을 사용하여 에러를 생성합니다.
    /// </summary>
    /// <param name="contextName">컨텍스트 이름</param>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    /// <remarks>
    /// Named Context 검증 패턴에서 사용됩니다.
    /// <code>
    /// DomainError.ForContext("ProductRegistration", new NotPositive(), value, "Amount must be positive");
    /// // Error: DomainErrors.ProductRegistration.NotPositive
    /// </code>
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error ForContext(
        string contextName,
        DomainErrorType errorType,
        string currentValue,
        string message) =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.DomainErrorsPrefix}.{contextName}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// 문자열 컨텍스트 이름을 사용하여 에러를 생성합니다. (제네릭 값 타입)
    /// </summary>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="contextName">컨텍스트 이름</param>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error ForContext<TValue>(
        string contextName,
        DomainErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.DomainErrorsPrefix}.{contextName}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);
}
