using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorKind;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public readonly partial struct ValidationContext
{
    /// <summary>
    /// 값이 null이 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">값의 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> NotNull<T>(T? value)
        where T : class =>
        new(NotNullInternal<T>(value), ContextName);

    /// <summary>
    /// nullable 값 타입이 null이 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">값의 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> NotNull<T>(T? value)
        where T : struct =>
        new(NotNullStructInternal<T>(value), ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> NotNullInternal<T>(T? value)
        where T : class =>
        value is not null
            ? value
            : DomainError.ForContext(
                ContextName,
                new Null(),
                "null",
                $"{ContextName} cannot be null.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> NotNullStructInternal<T>(T? value)
        where T : struct =>
        value.HasValue
            ? value.Value
            : DomainError.ForContext(
                ContextName,
                new Null(),
                "null",
                $"{ContextName} cannot be null.");
}
