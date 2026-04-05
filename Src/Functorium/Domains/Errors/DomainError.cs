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
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TDomain>(
        DomainErrorType errorType,
        string currentValue,
        string message) =>
        LayerErrorCore.Create<TDomain>(ErrorType.DomainErrorsPrefix, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TDomain, TValue>(
        DomainErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        LayerErrorCore.Create<TDomain, TValue>(ErrorType.DomainErrorsPrefix, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TDomain, T1, T2>(
        DomainErrorType errorType,
        T1 value1,
        T2 value2,
        string message)
        where T1 : notnull
        where T2 : notnull =>
        LayerErrorCore.Create<TDomain, T1, T2>(ErrorType.DomainErrorsPrefix, errorType, value1, value2, message);

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
        LayerErrorCore.Create<TDomain, T1, T2, T3>(ErrorType.DomainErrorsPrefix, errorType, value1, value2, value3, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error ForContext(
        string contextName,
        DomainErrorType errorType,
        string currentValue,
        string message) =>
        LayerErrorCore.ForContext(ErrorType.DomainErrorsPrefix, contextName, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error ForContext<TValue>(
        string contextName,
        DomainErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        LayerErrorCore.ForContext(ErrorType.DomainErrorsPrefix, contextName, errorType, currentValue, message);
}
