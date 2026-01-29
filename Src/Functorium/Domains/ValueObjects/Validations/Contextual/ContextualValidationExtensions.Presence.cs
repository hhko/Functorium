using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public static partial class ContextualValidationExtensions
{
    /// <summary>
    /// 값이 null이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">값의 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenNotNull<T>(
        this ContextualValidation<T?> validation)
        where T : class =>
        new(validation.Value.Bind(v => NotNullInternal<T>(v, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// nullable 값 타입이 null이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">값의 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenNotNull<T>(
        this ContextualValidation<T?> validation)
        where T : struct =>
        new(validation.Value.Bind(v => NotNullStructInternal<T>(v, validation.ContextName)), validation.ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> NotNullInternal<T>(T? value, string contextName)
        where T : class =>
        value is not null
            ? value
            : DomainError.ForContext(
                contextName,
                new Null(),
                "null",
                $"{contextName} cannot be null.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> NotNullStructInternal<T>(T? value, string contextName)
        where T : struct =>
        value.HasValue
            ? value.Value
            : DomainError.ForContext(
                contextName,
                new Null(),
                "null",
                $"{contextName} cannot be null.");
}
