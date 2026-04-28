using Framework.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;
using static LanguageExt.Prelude;

namespace Framework.Testing.Assertions.Errors;

/// <summary>
/// 범용 에러 코드 검증을 위한 확장 메서드
/// IHasErrorCode 인터페이스를 통해 에러 코드를 검증합니다.
/// </summary>
/// <remarks>
/// 이 클래스는 Chapter 14의 학습 목적에 맞춰 단순화된 버전입니다.
/// </remarks>
public static class ErrorCodeAssertions
{
    /// <summary>
    /// Validation에서 에러들을 추출합니다.
    /// </summary>
    private static Seq<Error> GetErrors<T>(Validation<Error, T> validation) =>
        validation.Match(
            Succ: _ => Empty,
            Fail: e => e.AsIterable().ToSeq());

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
    /// Validation이 성공 상태인지 검증하고 성공 값을 반환합니다.
    /// </summary>
    /// <typeparam name="T">성공 값의 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <returns>성공 값</returns>
    public static T ShouldBeValid<T>(this Validation<Error, T> validation)
    {
        if (validation.IsFail)
        {
            var errors = GetErrors(validation);
            var errorMessages = string.Join(", ", errors.Select(x => x.Message));
            throw new ShouldAssertException($"Validation should have succeeded, but failed with: {errorMessages}");
        }
        return validation.Match(Succ: v => v, Fail: _ => default!);
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

        var errors = GetErrors(validation);

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

        var errors = GetErrors(validation);

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors.First();
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

        var errors = GetErrors(validation);

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
