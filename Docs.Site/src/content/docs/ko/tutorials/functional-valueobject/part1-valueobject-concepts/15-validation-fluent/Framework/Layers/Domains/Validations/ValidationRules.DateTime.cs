using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;
using static Framework.Layers.Domains.DomainErrorKind;

namespace Framework.Layers.Domains.Validations;

public static partial class ValidationRules<TValueObject>
{
    /// <summary>
    /// 날짜가 기본값(DateTime.MinValue)이 아닌지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> NotDefault(DateTime value) =>
        new(NotDefaultInternal(value));

    /// <summary>
    /// 날짜가 과거인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> InPast(DateTime value) =>
        new(InPastInternal(value));

    /// <summary>
    /// 날짜가 미래인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> InFuture(DateTime value) =>
        new(InFutureInternal(value));

    /// <summary>
    /// 날짜가 특정 기준 날짜 이전인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> Before(DateTime value, DateTime boundary) =>
        new(BeforeInternal(value, boundary));

    /// <summary>
    /// 날짜가 특정 기준 날짜 이후인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> After(DateTime value, DateTime boundary) =>
        new(AfterInternal(value, boundary));

    /// <summary>
    /// 날짜가 지정된 범위 내에 있는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 날짜</param>
    /// <param name="min">최소 날짜</param>
    /// <param name="max">최대 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> DateBetween(DateTime value, DateTime min, DateTime max) =>
        new(DateBetweenInternal(value, min, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, DateTime> NotDefaultInternal(DateTime value) =>
        value != DateTime.MinValue
            ? value
            : DomainError.For<TValueObject, DateTime>(
                new DefaultDate(),
                value,
                $"{typeof(TValueObject).Name} date cannot be default (DateTime.MinValue). Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, DateTime> InPastInternal(DateTime value) =>
        value < DateTime.Now
            ? value
            : DomainError.For<TValueObject, DateTime>(
                new NotInPast(),
                value,
                $"{typeof(TValueObject).Name} must be in the past. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, DateTime> InFutureInternal(DateTime value) =>
        value > DateTime.Now
            ? value
            : DomainError.For<TValueObject, DateTime>(
                new NotInFuture(),
                value,
                $"{typeof(TValueObject).Name} must be in the future. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, DateTime> BeforeInternal(DateTime value, DateTime boundary) =>
        value < boundary
            ? value
            : DomainError.For<TValueObject, DateTime>(
                new TooLate(boundary.ToString("yyyy-MM-dd HH:mm:ss")),
                value,
                $"{typeof(TValueObject).Name} must be before {boundary:yyyy-MM-dd HH:mm:ss}. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, DateTime> AfterInternal(DateTime value, DateTime boundary) =>
        value > boundary
            ? value
            : DomainError.For<TValueObject, DateTime>(
                new TooEarly(boundary.ToString("yyyy-MM-dd HH:mm:ss")),
                value,
                $"{typeof(TValueObject).Name} must be after {boundary:yyyy-MM-dd HH:mm:ss}. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, DateTime> DateBetweenInternal(DateTime value, DateTime min, DateTime max) =>
        value >= min && value <= max
            ? value
            : DomainError.For<TValueObject, DateTime>(
                new OutOfRange(min.ToString("yyyy-MM-dd HH:mm:ss"), max.ToString("yyyy-MM-dd HH:mm:ss")),
                value,
                $"{typeof(TValueObject).Name} must be between {min:yyyy-MM-dd HH:mm:ss} and {max:yyyy-MM-dd HH:mm:ss}. Current value: '{value:yyyy-MM-dd HH:mm:ss}'");
}
