using System.Linq;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 예외 기반 에러(ErrorCodeExceptional) 검증을 위한 특화된 확장 메서드
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// // Error assertions
/// error.ShouldBeErrorCodeExceptional("Errors.Database.ConnectionFailed");
/// error.ShouldBeErrorCodeExceptional&lt;SqlException&gt;("Errors.Database.ConnectionFailed");
/// error.ShouldWrapException&lt;InvalidOperationException&gt;("Errors.Logic.InvalidState");
///
/// // Fin assertions
/// fin.ShouldFailWithException("Errors.Database.ConnectionFailed");
/// fin.ShouldFailWithException&lt;int, SqlException&gt;("Errors.Database.ConnectionFailed");
///
/// // Validation assertions
/// validation.ShouldContainException("Errors.Database.ConnectionFailed");
/// validation.ShouldContainException&lt;int, SqlException&gt;("Errors.Database.ConnectionFailed");
/// </code>
/// </remarks>
public static class ErrorCodeExceptionalAssertions
{
    #region Error Assertions

    /// <summary>
    /// Error가 ErrorCodeExceptional인지 검증합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldBeErrorCodeExceptional(
        this Error error,
        string expectedErrorCode)
    {
        error.IsExceptional.ShouldBeTrue($"Error should be Exceptional, but was Expected: {error.Message}");
        error.ShouldBeAssignableTo<IHasErrorCode>(
            $"Error should implement IHasErrorCode, but was {error.GetType().Name}");
        var hasErrorCode = (IHasErrorCode)error;
        hasErrorCode.ErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Error가 특정 예외 타입을 래핑한 ErrorCodeExceptional인지 검증합니다.
    /// </summary>
    /// <typeparam name="TException">기대하는 예외 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldBeErrorCodeExceptional<TException>(
        this Error error,
        string expectedErrorCode)
        where TException : Exception
    {
        error.ShouldBeErrorCodeExceptional(expectedErrorCode);
        error.HasException<TException>().ShouldBeTrue(
            $"Expected exception type {typeof(TException).Name}, but actual exception was {error.ToException().GetType().Name}");
    }

    /// <summary>
    /// Error가 특정 예외 타입을 래핑한 ErrorCodeExceptional인지 검증합니다 (예외 메시지 포함).
    /// </summary>
    /// <typeparam name="TException">기대하는 예외 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="expectedMessage">기대하는 예외 메시지 (선택)</param>
    public static void ShouldWrapException<TException>(
        this Error error,
        string expectedErrorCode,
        string? expectedMessage = null)
        where TException : Exception
    {
        error.ShouldBeErrorCodeExceptional<TException>(expectedErrorCode);

        if (expectedMessage != null)
        {
            error.Message.ShouldBe(expectedMessage);
        }
    }

    /// <summary>
    /// Error가 ErrorCodeExceptional이고 예외 검증 로직을 실행합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="exceptionAssertion">예외 검증 로직</param>
    public static void ShouldBeErrorCodeExceptional(
        this Error error,
        string expectedErrorCode,
        Action<Exception> exceptionAssertion)
    {
        error.ShouldBeErrorCodeExceptional(expectedErrorCode);
        exceptionAssertion(error.ToException());
    }

    #endregion

    #region Fin<T> Assertions

    /// <summary>
    /// Fin이 실패하고 ErrorCodeExceptional을 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldFailWithException<T>(
        this Fin<T> fin,
        string expectedErrorCode)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeErrorCodeExceptional(expectedErrorCode));
    }

    /// <summary>
    /// Fin이 실패하고 특정 예외 타입을 래핑한 ErrorCodeExceptional을 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <typeparam name="TException">기대하는 예외 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldFailWithException<T, TException>(
        this Fin<T> fin,
        string expectedErrorCode)
        where TException : Exception
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeErrorCodeExceptional<TException>(expectedErrorCode));
    }

    /// <summary>
    /// Fin이 실패하고 예외 검증 로직을 실행합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="exceptionAssertion">예외 검증 로직</param>
    public static void ShouldFailWithException<T>(
        this Fin<T> fin,
        string expectedErrorCode,
        Action<Exception> exceptionAssertion)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => error.ShouldBeErrorCodeExceptional(expectedErrorCode, exceptionAssertion));
    }

    #endregion

    #region Validation<Error, T> Assertions

    /// <summary>
    /// Validation에서 에러를 추출하여 List로 변환합니다.
    /// </summary>
    private static List<Error> ExtractErrors(Error error) =>
        error.AsIterable().ToList();

    /// <summary>
    /// Validation이 실패하고 특정 ErrorCodeExceptional을 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldContainException<T>(
        this Validation<Error, T> validation,
        string expectedErrorCode)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var matchingError = errors
            .Where(e => e.IsExceptional && e is IHasErrorCode hasErrorCode && hasErrorCode.ErrorCode == expectedErrorCode)
            .FirstOrDefault();

        matchingError.ShouldNotBeNull(
            $"Expected ErrorCodeExceptional with code '{expectedErrorCode}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    /// <summary>
    /// Validation이 실패하고 특정 예외 타입을 래핑한 ErrorCodeExceptional을 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <typeparam name="TException">기대하는 예외 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldContainException<T, TException>(
        this Validation<Error, T> validation,
        string expectedErrorCode)
        where TException : Exception
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var matchingError = errors
            .Where(e => e.IsExceptional &&
                       e is IHasErrorCode hasErrorCode &&
                       hasErrorCode.ErrorCode == expectedErrorCode &&
                       e.HasException<TException>())
            .FirstOrDefault();

        matchingError.ShouldNotBeNull(
            $"Expected ErrorCodeExceptional with code '{expectedErrorCode}' wrapping {typeof(TException).Name} not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    /// <summary>
    /// Validation이 실패하고 ErrorCodeExceptional이 정확히 하나만 있는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldContainOnlyException<T>(
        this Validation<Error, T> validation,
        string expectedErrorCode)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors[0];
        singleError.ShouldBeErrorCodeExceptional(expectedErrorCode);
    }

    #endregion
}
