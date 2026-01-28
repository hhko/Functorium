using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class ValidationRules<TValueObject>
{
    /// <summary>
    /// 배열이 비어있지 않은지 검증합니다.
    /// </summary>
    /// <typeparam name="TElement">배열 요소 타입</typeparam>
    /// <param name="value">검증할 배열</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, TElement[]> NotEmptyArray<TElement>(TElement[]? value) =>
        new(NotEmptyArrayInternal<TElement>(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, TElement[]> NotEmptyArrayInternal<TElement>(TElement[]? value)
    {
        var array = value ?? [];
        return array.Length > 0
            ? array
            : DomainError.For<TValueObject>(
                new Empty(),
                $"Length: {array.Length}",
                $"{typeof(TValueObject).Name} array cannot be empty or null. Current length: '{array.Length}'");
    }
}
