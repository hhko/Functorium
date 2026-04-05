using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Abstractions.Errors;

/// <summary>
/// 레이어별 Helper 클래스(DomainError, ApplicationError, AdapterError)의
/// 공통 에러 생성 로직을 제공하는 내부 구현 클래스
/// </summary>
internal static class LayerErrorCore
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error Create<TContext>(
        string prefix, ErrorType errorType, string currentValue, string message) =>
        ErrorCodeFactory.Create(
            $"{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}", currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error Create<TContext, TValue>(
        string prefix, ErrorType errorType, TValue currentValue, string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            $"{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}", currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error Create<TContext, T1, T2>(
        string prefix, ErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull
        where T2 : notnull =>
        ErrorCodeFactory.Create(
            $"{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}", value1, value2, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error Create<TContext, T1, T2, T3>(
        string prefix, ErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorCodeFactory.Create(
            $"{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}", value1, value2, value3, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error Create(
        string prefix, Type contextType, ErrorType errorType, string currentValue, string message) =>
        ErrorCodeFactory.Create(
            $"{prefix}.{contextType.Name}.{errorType.ErrorName}", currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error ForContext(
        string prefix, string contextName, ErrorType errorType, string currentValue, string message) =>
        ErrorCodeFactory.Create(
            $"{prefix}.{contextName}.{errorType.ErrorName}", currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error ForContext<TValue>(
        string prefix, string contextName, ErrorType errorType, TValue currentValue, string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            $"{prefix}.{contextName}.{errorType.ErrorName}", currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Error FromException<TContext>(
        string prefix, ErrorType errorType, Exception exception) =>
        ErrorCodeFactory.CreateFromException(
            $"{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}", exception);
}
