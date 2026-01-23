using System.Linq;
using Functorium.Abstractions.Errors;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;
using static LanguageExt.Prelude;

namespace Functorium.Testing.Assertions.Errors;

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
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <remarks>
    /// ErrorCodeExpected 또는 ErrorCodeExpected&lt;T&gt; 모두 지원합니다.
    /// DomainError.For&lt;T&gt;() 또는 DomainError.For&lt;T, TValue&gt;()로 생성된 에러 모두 검증 가능합니다.
    /// </remarks>
    public static void ShouldBeDomainError<TDomain>(
        this Error error,
        DomainErrorType expectedErrorType)
    {
        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";

        // ErrorCodeExpected 또는 ErrorCodeExpected<T> 모두 지원
        var actualErrorCode = error.ErrorCode;
        actualErrorCode.ShouldNotBeNull($"Error should be ErrorCodeExpected or ErrorCodeExpected<T>, but was {error.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Error가 특정 값 객체의 DomainError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeDomainError<TDomain, TValue>(
        this Error error,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<TValue>>();
        var errorCodeExpected = (ErrorCodeExpected<TValue>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    /// <summary>
    /// Error가 특정 값 객체의 DomainError인지 검증 (두 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    public static void ShouldBeDomainError<TDomain, T1, T2>(
        this Error error,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<T1, T2>>();
        var errorCodeExpected = (ErrorCodeExpected<T1, T2>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    /// <summary>
    /// Error가 특정 값 객체의 DomainError인지 검증 (세 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <typeparam name="T3">세 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    /// <param name="expectedValue3">기대하는 세 번째 값</param>
    public static void ShouldBeDomainError<TDomain, T1, T2, T3>(
        this Error error,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<T1, T2, T3>>();
        var errorCodeExpected = (ErrorCodeExpected<T1, T2, T3>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
        errorCodeExpected.ErrorCurrentValue3.ShouldBe(expectedValue3);
    }

    #endregion

    #region Fin<T> Assertions

    /// <summary>
    /// Fin 실패 결과가 특정 DomainError인지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldBeDomainError<TDomain, T>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeDomainError<TDomain>(expectedErrorType));
    }

    /// <summary>
    /// Fin 실패 결과가 특정 DomainError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeDomainError<TDomain, T, TValue>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeDomainError<TDomain, TValue>(expectedErrorType, expectedCurrentValue));
    }

    #endregion

    #region Validation<Error, T> Assertions

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <remarks>
    /// Validation이 실패 상태이고, 누적된 에러들 중 기대하는 DomainError가 포함되어 있는지 검증합니다.
    /// ManyErrors로 누적된 경우에도 올바르게 처리됩니다.
    /// </remarks>
    public static void ShouldHaveDomainError<TDomain, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        var hasMatchingError = errors.Any(e => e.ErrorCode == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    /// <summary>
    /// Validation 실패 결과가 정확히 해당 DomainError 하나만 포함하는지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldHaveOnlyDomainError<TDomain, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors[0];
        var actualErrorCode = singleError.ErrorCode;
        actualErrorCode.ShouldNotBeNull($"Error should be ErrorCodeExpected or ErrorCodeExpected<T>, but was {singleError.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Validation 실패 결과에 여러 DomainError가 모두 포함되어 있는지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorTypes">기대하는 에러 타입들</param>
    public static void ShouldHaveDomainErrors<TDomain, T>(
        this Validation<Error, T> validation,
        params DomainErrorType[] expectedErrorTypes)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCodes = expectedErrorTypes
            .Select(et => $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{et.ErrorName}")
            .ToList();

        var errors = validation.Errors;

        var actualErrorCodes = errors
            .Select(e => e.ErrorCode)
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
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldHaveDomainError<TDomain, T, TValue>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        var matchingError = errors
            .OfType<ErrorCodeExpected<TValue>>()
            .FirstOrDefault(e => e.ErrorCode == expectedErrorCode);

        matchingError.ShouldNotBeNull(
            $"Expected error code '{expectedErrorCode}' with value type '{typeof(TValue).Name}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");

        matchingError!.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증 (두 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    public static void ShouldHaveDomainError<TDomain, T, T1, T2>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        var matchingError = errors
            .OfType<ErrorCodeExpected<T1, T2>>()
            .FirstOrDefault(e => e.ErrorCode == expectedErrorCode);

        matchingError.ShouldNotBeNull(
            $"Expected error code '{expectedErrorCode}' with value types '{typeof(T1).Name}, {typeof(T2).Name}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");

        matchingError!.ErrorCurrentValue1.ShouldBe(expectedValue1);
        matchingError!.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증 (세 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <typeparam name="T3">세 번째 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    /// <param name="expectedValue3">기대하는 세 번째 값</param>
    public static void ShouldHaveDomainError<TDomain, T, T1, T2, T3>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        var matchingError = errors
            .OfType<ErrorCodeExpected<T1, T2, T3>>()
            .FirstOrDefault(e => e.ErrorCode == expectedErrorCode);

        matchingError.ShouldNotBeNull(
            $"Expected error code '{expectedErrorCode}' with value types '{typeof(T1).Name}, {typeof(T2).Name}, {typeof(T3).Name}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");

        matchingError!.ErrorCurrentValue1.ShouldBe(expectedValue1);
        matchingError!.ErrorCurrentValue2.ShouldBe(expectedValue2);
        matchingError!.ErrorCurrentValue3.ShouldBe(expectedValue3);
    }

    #endregion
}
