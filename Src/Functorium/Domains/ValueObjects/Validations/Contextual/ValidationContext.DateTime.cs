using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public readonly partial struct ValidationContext
{
    /// <summary>
    /// 날짜가 기본값(DateTime.MinValue)이 아닌지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<DateTime> NotDefault(DateTime value) =>
        new(NotDefaultInternal(value), ContextName);

    /// <summary>
    /// 날짜가 과거인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<DateTime> InPast(DateTime value) =>
        new(InPastInternal(value), ContextName);

    /// <summary>
    /// 날짜가 미래인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<DateTime> InFuture(DateTime value) =>
        new(InFutureInternal(value), ContextName);

    /// <summary>
    /// 날짜가 특정 기준 날짜 이전인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<DateTime> Before(DateTime value, DateTime boundary) =>
        new(BeforeInternal(value, boundary), ContextName);

    /// <summary>
    /// 날짜가 특정 기준 날짜 이후인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<DateTime> After(DateTime value, DateTime boundary) =>
        new(AfterInternal(value, boundary), ContextName);

    /// <summary>
    /// 날짜가 지정된 범위 내에 있는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <param name="min">최소 날짜</param>
    /// <param name="max">최대 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<DateTime> DateBetween(DateTime value, DateTime min, DateTime max) =>
        new(DateBetweenInternal(value, min, max), ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, DateTime> NotDefaultInternal(DateTime value) =>
        value != DateTime.MinValue
            ? value
            : DomainError.ForContext<DateTime>(
                ContextName,
                new DefaultDate(),
                value,
                $"{ContextName} date cannot be default (DateTime.MinValue). Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, DateTime> InPastInternal(DateTime value) =>
        value < DateTime.Now
            ? value
            : DomainError.ForContext<DateTime>(
                ContextName,
                new NotInPast(),
                value,
                $"{ContextName} must be in the past. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, DateTime> InFutureInternal(DateTime value) =>
        value > DateTime.Now
            ? value
            : DomainError.ForContext<DateTime>(
                ContextName,
                new NotInFuture(),
                value,
                $"{ContextName} must be in the future. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, DateTime> BeforeInternal(DateTime value, DateTime boundary) =>
        value < boundary
            ? value
            : DomainError.ForContext<DateTime>(
                ContextName,
                new TooLate(boundary.ToString("yyyy-MM-dd HH:mm:ss")),
                value,
                $"{ContextName} must be before {boundary:yyyy-MM-dd HH:mm:ss}. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, DateTime> AfterInternal(DateTime value, DateTime boundary) =>
        value > boundary
            ? value
            : DomainError.ForContext<DateTime>(
                ContextName,
                new TooEarly(boundary.ToString("yyyy-MM-dd HH:mm:ss")),
                value,
                $"{ContextName} must be after {boundary:yyyy-MM-dd HH:mm:ss}. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, DateTime> DateBetweenInternal(DateTime value, DateTime min, DateTime max) =>
        value >= min && value <= max
            ? value
            : DomainError.ForContext<DateTime>(
                ContextName,
                new OutOfRange(min.ToString("yyyy-MM-dd HH:mm:ss"), max.ToString("yyyy-MM-dd HH:mm:ss")),
                value,
                $"{ContextName} must be between {min:yyyy-MM-dd HH:mm:ss} and {max:yyyy-MM-dd HH:mm:ss}. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");
}
