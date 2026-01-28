using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class ValidationRules<TValueObject>
{
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
}
