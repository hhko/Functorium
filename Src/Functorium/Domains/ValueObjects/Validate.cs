using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects;

/// <summary>
/// 타입 파라미터를 한 번만 지정하는 검증 시작점
/// </summary>
/// <typeparam name="TValueObject">값 객체 타입</typeparam>
public static class Validate<TValueObject>
{
    /// <summary>
    /// 문자열이 비어있지 않은지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> NotEmpty(string value) =>
        new(NotEmptyInternal(value));

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> MinLength(string value, int minLength) =>
        new(MinLengthInternal(value, minLength));

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> MaxLength(string value, int maxLength) =>
        new(MaxLengthInternal(value, maxLength));

    /// <summary>
    /// 문자열이 정확한 길이인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ExactLength(string value, int length) =>
        new(ExactLengthInternal(value, length));

    /// <summary>
    /// 문자열이 정규식 패턴과 일치하는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="pattern">정규식 패턴</param>
    /// <param name="message">오류 메시지 (선택적)</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> Matches(
        string value,
        Regex pattern,
        string? message = null) =>
        new(MatchesInternal(value, pattern, message));

    /// <summary>
    /// 숫자가 음수가 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> NonNegative<T>(T value)
        where T : notnull, INumber<T> =>
        new(NonNegativeInternal<T>(value));

    /// <summary>
    /// 숫자가 양수인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> Positive<T>(T value)
        where T : notnull, INumber<T> =>
        new(PositiveInternal<T>(value));

    /// <summary>
    /// 숫자가 지정된 범위 내에 있는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> Between<T>(T value, T min, T max)
        where T : notnull, INumber<T> =>
        new(BetweenInternal<T>(value, min, max));

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> AtMost<T>(T value, T max)
        where T : notnull, INumber<T> =>
        new(AtMostInternal<T>(value, max));

    /// <summary>
    /// 숫자가 최소값 이상인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> AtLeast<T>(T value, T min)
        where T : notnull, INumber<T> =>
        new(AtLeastInternal<T>(value, min));

    /// <summary>
    /// 사용자 정의 조건으로 값을 검증합니다.
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="predicate">검증 조건</param>
    /// <param name="errorType">에러 타입</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> Must<T>(
        T value,
        Func<T, bool> predicate,
        DomainErrorType errorType,
        string message)
        where T : notnull =>
        new(MustInternal(value, predicate, errorType, message));

    #region Internal Validation Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> NotEmptyInternal(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<TValueObject>(
                new Empty(),
                value,
                $"{typeof(TValueObject).Name} cannot be empty. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> MinLengthInternal(string value, int minLength) =>
        value.Length >= minLength
            ? value
            : DomainError.For<TValueObject>(
                new TooShort(minLength),
                value,
                $"{typeof(TValueObject).Name} must be at least {minLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> MaxLengthInternal(string value, int maxLength) =>
        value.Length <= maxLength
            ? value
            : DomainError.For<TValueObject>(
                new TooLong(maxLength),
                value,
                $"{typeof(TValueObject).Name} must not exceed {maxLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> ExactLengthInternal(string value, int length) =>
        value.Length == length
            ? value
            : DomainError.For<TValueObject>(
                new WrongLength(length),
                value,
                $"{typeof(TValueObject).Name} must be exactly {length} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> MatchesInternal(
        string value,
        Regex pattern,
        string? message = null) =>
        pattern.IsMatch(value)
            ? value
            : DomainError.For<TValueObject>(
                new InvalidFormat(pattern.ToString()),
                value,
                message ?? $"Invalid {typeof(TValueObject).Name} format. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> NonNegativeInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value >= T.Zero
            ? value
            : DomainError.For<TValueObject, T>(
                new Negative(),
                value,
                $"{typeof(TValueObject).Name} cannot be negative. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> PositiveInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value > T.Zero
            ? value
            : DomainError.For<TValueObject, T>(
                new NotPositive(),
                value,
                $"{typeof(TValueObject).Name} must be positive. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> BetweenInternal<T>(T value, T min, T max)
        where T : notnull, INumber<T> =>
        value >= min && value <= max
            ? value
            : DomainError.For<TValueObject, T>(
                new OutOfRange(min.ToString(), max.ToString()),
                value,
                $"{typeof(TValueObject).Name} must be between {min} and {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> AtMostInternal<T>(T value, T max)
        where T : notnull, INumber<T> =>
        value <= max
            ? value
            : DomainError.For<TValueObject, T>(
                new AboveMaximum(max.ToString()),
                value,
                $"{typeof(TValueObject).Name} cannot exceed {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> AtLeastInternal<T>(T value, T min)
        where T : notnull, INumber<T> =>
        value >= min
            ? value
            : DomainError.For<TValueObject, T>(
                new BelowMinimum(min.ToString()),
                value,
                $"{typeof(TValueObject).Name} must be at least {min}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> MustInternal<T>(
        T value,
        Func<T, bool> predicate,
        DomainErrorType errorType,
        string message)
        where T : notnull =>
        predicate(value)
            ? value
            : DomainError.For<TValueObject, T>(
                errorType,
                value,
                message);

    #endregion
}
