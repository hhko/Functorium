using System.Linq;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 범용 에러 코드 검증을 위한 확장 메서드
/// DomainErrorType에 의존하지 않고 순수 ErrorCode 문자열 기반으로 동작합니다.
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// // Error 상태 검증
/// error.ShouldHaveErrorCode();
/// error.ShouldBeExpected();
/// error.ShouldBeExceptional();
///
/// // ErrorCode 매칭
/// error.ShouldHaveErrorCode("Domain.Email.Empty");
/// error.ShouldHaveErrorCodeStartingWith("Domain.Email");
/// error.ShouldHaveErrorCode(code => code.Contains("Email"));
///
/// // ExpectedError 검증
/// error.ShouldBeExpectedError("Domain.Email.Empty", "");
/// error.ShouldBeExpectedError&lt;int&gt;("Domain.Age.Negative", -5);
///
/// // Fin 검증
/// fin.ShouldSucceed();
/// fin.ShouldFailWithErrorCode("Domain.Email.Empty");
///
/// // Validation 검증
/// validation.ShouldBeValid();
/// validation.ShouldContainErrorCode("Domain.Email.Empty");
/// </code>
/// </remarks>
public static class ExpectedErrorAssertions
{
    #region Error State Assertions

    /// <summary>
    /// Error가 IHasErrorCode 인터페이스를 구현하는지 검증하고 해당 인터페이스를 반환합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <returns>IHasErrorCode 인터페이스</returns>
    public static IHasErrorCode ShouldHaveErrorCode(this Error error)
    {
        error.ShouldBeAssignableTo<IHasErrorCode>(
            $"Error should implement IHasErrorCode, but was {error.GetType().Name}");
        return (IHasErrorCode)error;
    }

    /// <summary>
    /// Error가 Expected 타입인지 검증합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    public static void ShouldBeExpected(this Error error)
    {
        error.IsExpected.ShouldBeTrue($"Error should be Expected, but was Exceptional: {error.Message}");
    }

    /// <summary>
    /// Error가 Exceptional 타입인지 검증합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    public static void ShouldBeExceptional(this Error error)
    {
        error.IsExceptional.ShouldBeTrue($"Error should be Exceptional, but was Expected: {error.Message}");
    }

    #endregion

    #region ErrorCode Matching Assertions

    /// <summary>
    /// Error가 특정 ErrorCode를 가지는지 검증합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldHaveErrorCode(this Error error, string expectedErrorCode)
    {
        var hasErrorCode = error.ShouldHaveErrorCode();
        hasErrorCode.ErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Error의 ErrorCode가 특정 접두사로 시작하는지 검증합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <param name="prefix">기대하는 접두사</param>
    public static void ShouldHaveErrorCodeStartingWith(this Error error, string prefix)
    {
        var hasErrorCode = error.ShouldHaveErrorCode();
        hasErrorCode.ErrorCode.ShouldStartWith(prefix);
    }

    /// <summary>
    /// Error의 ErrorCode가 predicate 조건을 만족하는지 검증합니다.
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <param name="predicate">검증 조건</param>
    /// <param name="customMessage">실패 시 메시지 (선택)</param>
    public static void ShouldHaveErrorCode(
        this Error error,
        Func<string, bool> predicate,
        string? customMessage = null)
    {
        var hasErrorCode = error.ShouldHaveErrorCode();
        predicate(hasErrorCode.ErrorCode).ShouldBeTrue(
            customMessage ?? $"Error code '{hasErrorCode.ErrorCode}' did not match the predicate");
    }

    #endregion

    #region ExpectedError Variants Assertions

    /// <summary>
    /// Error가 특정 ExpectedError인지 검증합니다 (비제네릭).
    /// </summary>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeExpectedError(
        this Error error,
        string expectedErrorCode,
        string expectedCurrentValue)
    {
        error.ShouldBeOfType<ExpectedError>();
        var errorCodeExpected = (ExpectedError)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    /// <summary>
    /// Error가 특정 ExpectedError&lt;T&gt;인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">현재 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeExpectedError<T>(
        this Error error,
        string expectedErrorCode,
        T expectedCurrentValue)
        where T : notnull
    {
        error.ShouldBeOfType<ExpectedError<T>>();
        var errorCodeExpected = (ExpectedError<T>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    /// <summary>
    /// Error가 특정 ExpectedError&lt;T&gt;인지 검증합니다 (predicate 기반).
    /// </summary>
    /// <typeparam name="T">현재 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="valuePredicate">값 검증 조건</param>
    /// <param name="customMessage">실패 시 메시지 (선택)</param>
    public static void ShouldBeExpectedError<T>(
        this Error error,
        string expectedErrorCode,
        Func<T, bool> valuePredicate,
        string? customMessage = null)
        where T : notnull
    {
        error.ShouldBeOfType<ExpectedError<T>>();
        var errorCodeExpected = (ExpectedError<T>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        valuePredicate(errorCodeExpected.ErrorCurrentValue).ShouldBeTrue(
            customMessage ?? $"Value '{errorCodeExpected.ErrorCurrentValue}' did not match the predicate");
    }

    /// <summary>
    /// Error가 특정 ExpectedError&lt;T1, T2&gt;인지 검증합니다.
    /// </summary>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    public static void ShouldBeExpectedError<T1, T2>(
        this Error error,
        string expectedErrorCode,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        error.ShouldBeOfType<ExpectedError<T1, T2>>();
        var errorCodeExpected = (ExpectedError<T1, T2>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    /// <summary>
    /// Error가 특정 ExpectedError&lt;T1, T2, T3&gt;인지 검증합니다.
    /// </summary>
    /// <typeparam name="T1">첫 번째 값의 타입</typeparam>
    /// <typeparam name="T2">두 번째 값의 타입</typeparam>
    /// <typeparam name="T3">세 번째 값의 타입</typeparam>
    /// <param name="error">검증할 Error</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    /// <param name="expectedValue1">기대하는 첫 번째 값</param>
    /// <param name="expectedValue2">기대하는 두 번째 값</param>
    /// <param name="expectedValue3">기대하는 세 번째 값</param>
    public static void ShouldBeExpectedError<T1, T2, T3>(
        this Error error,
        string expectedErrorCode,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        error.ShouldBeOfType<ExpectedError<T1, T2, T3>>();
        var errorCodeExpected = (ExpectedError<T1, T2, T3>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
        errorCodeExpected.ErrorCurrentValue3.ShouldBe(expectedValue3);
    }

    #endregion

    #region Fin<T> Assertions

    /// <summary>
    /// Fin이 성공 상태인지 검증하고 성공 값을 반환합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <returns>성공 값</returns>
    public static T ShouldSucceed<T>(this Fin<T> fin)
    {
        fin.IsSucc.ShouldBeTrue(
            fin.Match(
                Succ: _ => "Fin should have succeeded",
                Fail: e => $"Fin should have succeeded, but failed with: {e.Message}"));
        return fin.Match(Succ: v => v, Fail: _ => default!);
    }

    /// <summary>
    /// Fin이 성공하고 특정 값을 가지는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedValue">기대하는 값</param>
    public static void ShouldSucceedWith<T>(this Fin<T> fin, T expectedValue)
    {
        var actualValue = fin.ShouldSucceed();
        actualValue.ShouldBe(expectedValue);
    }

    /// <summary>
    /// Fin이 성공하고 predicate 조건을 만족하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="predicate">검증 조건</param>
    /// <param name="customMessage">실패 시 메시지 (선택)</param>
    public static void ShouldSucceed<T>(
        this Fin<T> fin,
        Func<T, bool> predicate,
        string? customMessage = null)
    {
        var actualValue = fin.ShouldSucceed();
        predicate(actualValue).ShouldBeTrue(
            customMessage ?? $"Success value '{actualValue}' did not match the predicate");
    }

    /// <summary>
    /// Fin이 실패 상태인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    public static void ShouldFail<T>(this Fin<T> fin)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
    }

    /// <summary>
    /// Fin이 실패하고 에러 assertion을 수행합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="errorAssertion">에러 검증 로직</param>
    public static void ShouldFail<T>(this Fin<T> fin, Action<Error> errorAssertion)
    {
        fin.ShouldFail();
        fin.IfFail(errorAssertion);
    }

    /// <summary>
    /// Fin이 실패하고 특정 ErrorCode를 가지는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldFailWithErrorCode<T>(this Fin<T> fin, string expectedErrorCode)
    {
        fin.ShouldFail(error => error.ShouldHaveErrorCode(expectedErrorCode));
    }

    #endregion

    #region Validation<Error, T> Assertions

    /// <summary>
    /// Validation에서 에러를 추출하여 List로 변환합니다.
    /// </summary>
    private static List<Error> ExtractErrors(Error error) =>
        error.AsIterable().ToList();

    /// <summary>
    /// Validation이 성공 상태인지 검증하고 성공 값을 반환합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <returns>성공 값</returns>
    public static T ShouldBeValid<T>(this Validation<Error, T> validation)
    {
        if (validation.IsFail)
        {
            var error = (Error)validation;
            var errors = ExtractErrors(error);
            var errorMessages = string.Join(", ", errors.Select(x => x.Message));
            throw new ShouldAssertException($"Validation should have succeeded, but failed with: {errorMessages}");
        }
        return validation.Match(Succ: v => v, Fail: _ => default!);
    }

    /// <summary>
    /// Validation이 실패하고 에러 assertion을 수행합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="errorsAssertion">에러 목록 검증 로직</param>
    public static void ShouldBeInvalid<T>(
        this Validation<Error, T> validation,
        Action<Seq<Error>> errorsAssertion)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");
        var error = (Error)validation;
        var errors = error.AsIterable().ToSeq();
        errorsAssertion(errors);
    }

    /// <summary>
    /// Validation이 실패하고 특정 ErrorCode를 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldContainErrorCode<T>(
        this Validation<Error, T> validation,
        string expectedErrorCode)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var hasMatchingError = errors.Any(e =>
            e is IHasErrorCode hasErrorCode && hasErrorCode.ErrorCode == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    /// <summary>
    /// Validation이 실패하고 정확히 해당 ErrorCode 하나만 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorCode">기대하는 에러 코드</param>
    public static void ShouldContainOnlyErrorCode<T>(
        this Validation<Error, T> validation,
        string expectedErrorCode)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors[0];
        singleError.ShouldBeAssignableTo<IHasErrorCode>();
        ((IHasErrorCode)singleError).ErrorCode.ShouldBe(expectedErrorCode);
    }

    /// <summary>
    /// Validation이 실패하고 여러 ErrorCode를 모두 포함하는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorCodes">기대하는 에러 코드들</param>
    public static void ShouldContainErrorCodes<T>(
        this Validation<Error, T> validation,
        params string[] expectedErrorCodes)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var error = (Error)validation;
        var errors = ExtractErrors(error);

        var actualErrorCodes = errors
            .OfType<IHasErrorCode>()
            .Select(e => e.ErrorCode)
            .ToList();

        foreach (var expectedCode in expectedErrorCodes)
        {
            actualErrorCodes.ShouldContain(expectedCode,
                $"Expected error code '{expectedCode}' not found. " +
                $"Actual error codes: [{string.Join(", ", actualErrorCodes)}]");
        }
    }

    #endregion
}
