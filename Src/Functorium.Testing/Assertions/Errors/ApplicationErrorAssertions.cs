using System.Linq;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Errors;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 애플리케이션 에러 검증을 위한 타입 안전 확장 메서드
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// // Error 검증
/// error.ShouldBeApplicationError&lt;CreateProductCommand&gt;(new ApplicationErrorType.AlreadyExists());
/// error.ShouldBeApplicationError&lt;UpdateOrderCommand, int&gt;(new ApplicationErrorType.NotFound(), orderId);
///
/// // Fin 결과 검증
/// fin.ShouldBeApplicationError&lt;CreateProductCommand&gt;(new ApplicationErrorType.ValidationFailed());
///
/// // Validation 결과 검증
/// validation.ShouldHaveApplicationError&lt;CreateProductCommand&gt;(new ApplicationErrorType.AlreadyExists());
/// </code>
/// </remarks>
public static class ApplicationErrorAssertions
{
    #region Error Assertions

    /// <summary>
    /// Error가 특정 유스케이스의 ApplicationError인지 검증
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldBeApplicationError<TUsecase>(
        this Error error,
        ApplicationErrorType expectedErrorType)
    {
        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";

        var actualErrorCode = error.ErrorCode;
        actualErrorCode.ShouldNotBeNull($"Error should be ErrorCodeExpected or ErrorCodeExpected<T>, but was {error.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Error가 특정 유스케이스의 ApplicationError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeApplicationError<TUsecase, TValue>(
        this Error error,
        ApplicationErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<TValue>>();
        var errorCodeExpected = (ErrorCodeExpected<TValue>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    /// <summary>
    /// Error가 특정 유스케이스의 ApplicationError인지 검증 (두 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    public static void ShouldBeApplicationError<TUsecase, T1, T2>(
        this Error error,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";

        error.ShouldBeOfType<ErrorCodeExpected<T1, T2>>();
        var errorCodeExpected = (ErrorCodeExpected<T1, T2>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    /// <summary>
    /// Error가 특정 유스케이스의 ApplicationError인지 검증 (세 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <typeparam name="T3">세 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    /// <param name="expectedValue3">기대하는 세 번째 값</param>
    public static void ShouldBeApplicationError<TUsecase, T1, T2, T3>(
        this Error error,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";

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
    /// Fin 실패 결과가 특정 ApplicationError인지 검증
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldBeApplicationError<TUsecase, T>(
        this Fin<T> fin,
        ApplicationErrorType expectedErrorType)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeApplicationError<TUsecase>(expectedErrorType));
    }

    /// <summary>
    /// Fin 실패 결과가 특정 ApplicationError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeApplicationError<TUsecase, T, TValue>(
        this Fin<T> fin,
        ApplicationErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeApplicationError<TUsecase, TValue>(expectedErrorType, expectedCurrentValue));
    }

    #endregion

    #region Validation<Error, T> Assertions

    /// <summary>
    /// Validation 실패 결과가 특정 ApplicationError를 포함하는지 검증
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldHaveApplicationError<TUsecase, T>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        var hasMatchingError = errors.Any(e => e.ErrorCode == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    /// <summary>
    /// Validation 실패 결과가 정확히 해당 ApplicationError 하나만 포함하는지 검증
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldHaveOnlyApplicationError<TUsecase, T>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";
        var errors = validation.Errors;

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors[0];
        var actualErrorCode = singleError.ErrorCode;
        actualErrorCode.ShouldNotBeNull($"Error should be ErrorCodeExpected or ErrorCodeExpected<T>, but was {singleError.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Validation 실패 결과에 여러 ApplicationError가 모두 포함되어 있는지 검증
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorTypes">기대하는 에러 타입들</param>
    public static void ShouldHaveApplicationErrors<TUsecase, T>(
        this Validation<Error, T> validation,
        params ApplicationErrorType[] expectedErrorTypes)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCodes = expectedErrorTypes
            .Select(et => $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{et.ErrorName}")
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
    /// Validation 실패 결과가 특정 ApplicationError를 포함하는지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldHaveApplicationError<TUsecase, T, TValue>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";
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
    /// Validation 실패 결과가 특정 ApplicationError를 포함하는지 검증 (두 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    public static void ShouldHaveApplicationError<TUsecase, T, T1, T2>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";
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
    /// Validation 실패 결과가 특정 ApplicationError를 포함하는지 검증 (세 개의 현재 값 포함)
    /// </summary>
    /// <typeparam name="TUsecase">유스케이스 타입</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <typeparam name="T3">세 번째 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    /// <param name="expectedValue3">기대하는 세 번째 값</param>
    public static void ShouldHaveApplicationError<TUsecase, T, T1, T2, T3>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TUsecase).Name}.{expectedErrorType.ErrorName}";
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
