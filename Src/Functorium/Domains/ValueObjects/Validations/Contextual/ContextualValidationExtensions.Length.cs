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
    /// 문자열이 비어있지 않은지 체인으로 검증합니다.
    /// </summary>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<string> ThenNotEmpty(
        this ContextualValidation<string> validation) =>
        new(validation.Value.Bind(v => NotEmptyInternal(v, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 체인으로 검증합니다.
    /// </summary>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<string> ThenMinLength(
        this ContextualValidation<string> validation,
        int minLength) =>
        new(validation.Value.Bind(v => MinLengthInternal(v, minLength, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<string> ThenMaxLength(
        this ContextualValidation<string> validation,
        int maxLength) =>
        new(validation.Value.Bind(v => MaxLengthInternal(v, maxLength, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 문자열이 정확한 길이인지 체인으로 검증합니다.
    /// </summary>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<string> ThenExactLength(
        this ContextualValidation<string> validation,
        int length) =>
        new(validation.Value.Bind(v => ExactLengthInternal(v, length, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 문자열을 변환(정규화)합니다.
    /// <para>
    /// <b>배치 규칙</b>: 존재성 검사(NotNull, NotEmpty) 직후, 구조적 검사(MinLength, MaxLength, Matches) 이전에
    /// 배치하십시오. 구조적 검증이 정규화된 값에 대해 수행되도록 하기 위함입니다.
    /// </para>
    /// </summary>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="normalize">변환 함수</param>
    /// <returns>변환된 문자열을 포함하는 검증 결과</returns>
    /// <example>
    /// <code>
    /// ContextualValidationRules
    ///     .NotNull(value, "FieldName")
    ///     .ThenNotEmpty()
    ///     .ThenNormalize(v =&gt; v.Trim())    // 존재성 검사 직후
    ///     .ThenMaxLength(100);                // 정규화된 값으로 검증
    /// </code>
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<string> ThenNormalize(
        this ContextualValidation<string> validation,
        Func<string, string> normalize) =>
        new(validation.Value.Map(normalize), validation.ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, string> NotEmptyInternal(string value, string contextName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.ForContext(
                contextName,
                new Empty(),
                value,
                $"{contextName} cannot be empty. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, string> MinLengthInternal(string value, int minLength, string contextName) =>
        value.Length >= minLength
            ? value
            : DomainError.ForContext(
                contextName,
                new TooShort(minLength),
                value,
                $"{contextName} must be at least {minLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, string> MaxLengthInternal(string value, int maxLength, string contextName) =>
        value.Length <= maxLength
            ? value
            : DomainError.ForContext(
                contextName,
                new TooLong(maxLength),
                value,
                $"{contextName} must not exceed {maxLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, string> ExactLengthInternal(string value, int length, string contextName) =>
        value.Length == length
            ? value
            : DomainError.ForContext(
                contextName,
                new WrongLength(length),
                value,
                $"{contextName} must be exactly {length} characters. Current length: {value.Length}");
}
