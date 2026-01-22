using System.Linq;
using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;
using static LanguageExt.Prelude;

namespace Functorium.Testing.Assertions;

/// <summary>
/// 도메인 에러 검증을 위한 타입 안전 확장 메서드
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// // Error 검증
/// error.ShouldBeDomainError&lt;Email&gt;(new DomainErrorType.Empty());
/// error.ShouldBeDomainError&lt;Age, int&gt;(new DomainErrorType.Negative(), -5);
///
/// // Fin 결과 검증
/// fin.ShouldBeDomainError&lt;Email&gt;(new DomainErrorType.InvalidFormat());
///
/// // Validation 결과 검증
/// validation.ShouldHaveDomainError&lt;Email&gt;(new DomainErrorType.Empty());
/// </code>
/// </remarks>
public static class DomainErrorAssertions
{
    #region Error Assertions

    /// <summary>
    /// Error가 특정 값 객체의 DomainError인지 검증
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <remarks>
    /// ErrorCodeExpected 또는 ErrorCodeExpected&lt;T&gt; 모두 지원합니다.
    /// DomainError.For&lt;T&gt;() 또는 DomainError.For&lt;T, TValue&gt;()로 생성된 에러 모두 검증 가능합니다.
    /// </remarks>
    public static void ShouldBeDomainError<TValueObject>(
        this Error error,
        DomainErrorType expectedErrorType)
    {
        var expectedErrorCode = $"DomainErrors.{typeof(TValueObject).Name}.{expectedErrorType.ErrorName}";

        // ErrorCodeExpected 또는 ErrorCodeExpected<T> 모두 지원
        var actualErrorCode = ExtractErrorCode(error);
        actualErrorCode.ShouldNotBeNull($"Error should be ErrorCodeExpected or ErrorCodeExpected<T>, but was {error.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Error에서 ErrorCode를 추출합니다. ErrorCodeExpected 또는 ErrorCodeExpected&lt;T&gt; 지원.
    /// </summary>
    private static string? ExtractErrorCode(Error error) =>
        error switch
        {
            ErrorCodeExpected ece => ece.ErrorCode,
            _ when error.GetType().IsGenericType &&
                   error.GetType().GetGenericTypeDefinition().Name.StartsWith("ErrorCodeExpected") =>
                (string?)error.GetType().GetProperty("ErrorCode")?.GetValue(error),
            _ => null
        };

    /// <summary>
    /// Error가 특정 값 객체의 DomainError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeDomainError<TValueObject, TValue>(
        this Error error,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        var expectedErrorCode = $"DomainErrors.{typeof(TValueObject).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<TValue>>();
        var errorCodeExpected = (ErrorCodeExpected<TValue>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    /// <summary>
    /// Error가 특정 값 객체의 DomainError인지 검증 (두 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    public static void ShouldBeDomainError<TValueObject, T1, T2>(
        this Error error,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        var expectedErrorCode = $"DomainErrors.{typeof(TValueObject).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<T1, T2>>();
        var errorCodeExpected = (ErrorCodeExpected<T1, T2>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    #endregion

    #region Fin<T> Assertions

    /// <summary>
    /// Fin 실패 결과가 특정 DomainError인지 검증
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldBeDomainError<TValueObject, T>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeDomainError<TValueObject>(expectedErrorType));
    }

    /// <summary>
    /// Fin 실패 결과가 특정 DomainError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeDomainError<TValueObject, T, TValue>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeDomainError<TValueObject, TValue>(expectedErrorType, expectedCurrentValue));
    }

    #endregion

    #region Validation<Error, T> Assertions

    /// <summary>
    /// Validation에서 에러를 추출하여 List로 변환합니다.
    /// ManyErrors인 경우 모든 에러를, 단일 에러인 경우 해당 에러만 반환합니다.
    /// </summary>
    private static List<Error> ExtractErrors(Error error) =>
        error.AsIterable().ToList();

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <remarks>
    /// Validation이 실패 상태이고, 누적된 에러들 중 기대하는 DomainError가 포함되어 있는지 검증합니다.
    /// ManyErrors로 누적된 경우에도 올바르게 처리됩니다.
    /// </remarks>
    public static void ShouldHaveDomainError<TValueObject, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"DomainErrors.{typeof(TValueObject).Name}.{expectedErrorType.ErrorName}";

        // 명시적 캐스트로 에러 추출 (IsFail 검증 후이므로 안전)
        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var hasMatchingError = errors.Any(e =>
            ExtractErrorCode(e) == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    /// <summary>
    /// Validation 실패 결과가 정확히 해당 DomainError 하나만 포함하는지 검증
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldHaveOnlyDomainError<TValueObject, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"DomainErrors.{typeof(TValueObject).Name}.{expectedErrorType.ErrorName}";

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors[0];
        var actualErrorCode = ExtractErrorCode(singleError);
        actualErrorCode.ShouldNotBeNull($"Error should be ErrorCodeExpected or ErrorCodeExpected<T>, but was {singleError.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Validation 실패 결과에 여러 DomainError가 모두 포함되어 있는지 검증
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorTypes">기대하는 에러 타입들</param>
    public static void ShouldHaveDomainErrors<TValueObject, T>(
        this Validation<Error, T> validation,
        params DomainErrorType[] expectedErrorTypes)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCodes = expectedErrorTypes
            .Select(et => $"DomainErrors.{typeof(TValueObject).Name}.{et.ErrorName}")
            .ToList();

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var actualErrorCodes = errors
            .Select(e => ExtractErrorCode(e))
            .Where(code => code != null)
            .ToList();

        foreach (var expectedCode in expectedErrorCodes)
        {
            actualErrorCodes.ShouldContain(expectedCode,
                $"Expected error code '{expectedCode}' not found. " +
                $"Actual error codes: [{string.Join(", ", actualErrorCodes)}]");
        }
    }

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldHaveDomainError<TValueObject, T, TValue>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"DomainErrors.{typeof(TValueObject).Name}.{expectedErrorType.ErrorName}";

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var matchingError = errors
            .OfType<ErrorCodeExpected<TValue>>()
            .FirstOrDefault(e => e.ErrorCode == expectedErrorCode);

        matchingError.ShouldNotBeNull(
            $"Expected error code '{expectedErrorCode}' with value type '{typeof(TValue).Name}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");

        matchingError!.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    #endregion
}
