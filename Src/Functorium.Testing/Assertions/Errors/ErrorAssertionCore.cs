using System.Linq;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 레이어별 Assertion 클래스(DomainErrorAssertions, ApplicationErrorAssertions, AdapterErrorAssertions)의
/// 공통 검증 로직을 제공하는 내부 구현 클래스
/// </summary>
internal static class ErrorAssertionCore
{
    #region Error Assertions

    internal static void ShouldBeError<TContext>(
        Error error, string prefix, string errorName)
    {
        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";

        var actualErrorCode = error.ErrorCode;
        actualErrorCode.ShouldNotBeNull($"Error should be ExpectedError or ExpectedError<T>, but was {error.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    internal static void ShouldBeError<TContext, TValue>(
        Error error, string prefix, string errorName, TValue expectedCurrentValue)
        where TValue : notnull
    {
        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";

        error.ShouldBeOfType<ExpectedError<TValue>>();
        var errorCodeExpected = (ExpectedError<TValue>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    internal static void ShouldBeError<TContext, T1, T2>(
        Error error, string prefix, string errorName,
        T1 expectedValue1, T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";

        error.ShouldBeOfType<ExpectedError<T1, T2>>();
        var errorCodeExpected = (ExpectedError<T1, T2>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    internal static void ShouldBeError<TContext, T1, T2, T3>(
        Error error, string prefix, string errorName,
        T1 expectedValue1, T2 expectedValue2, T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";

        error.ShouldBeOfType<ExpectedError<T1, T2, T3>>();
        var errorCodeExpected = (ExpectedError<T1, T2, T3>)error;
        errorCodeExpected.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedValue2);
        errorCodeExpected.ErrorCurrentValue3.ShouldBe(expectedValue3);
    }

    internal static void ShouldBeExceptionalError<TContext>(
        Error error, string prefix, string errorName)
    {
        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";

        error.ShouldBeOfType<ExceptionalError>();
        var errorCodeExceptional = (ExceptionalError)error;
        errorCodeExceptional.ErrorCode.ShouldBe(expectedErrorCode);
    }

    internal static void ShouldBeExceptionalError<TContext, TException>(
        Error error, string prefix, string errorName)
        where TException : Exception
    {
        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";

        error.ShouldBeOfType<ExceptionalError>();
        var errorCodeExceptional = (ExceptionalError)error;
        errorCodeExceptional.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExceptional.ToException().ShouldBeOfType<TException>();
    }

    #endregion

    #region Fin<T> Assertions

    internal static void ShouldBeFinError<TContext, T>(
        Fin<T> fin, string prefix, string errorName)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => ShouldBeError<TContext>(error, prefix, errorName));
    }

    internal static void ShouldBeFinError<TContext, T, TValue>(
        Fin<T> fin, string prefix, string errorName, TValue expectedCurrentValue)
        where TValue : notnull
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => ShouldBeError<TContext, TValue>(error, prefix, errorName, expectedCurrentValue));
    }

    internal static void ShouldBeFinExceptionalError<TContext, T>(
        Fin<T> fin, string prefix, string errorName)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");
        fin.IfFail(error => ShouldBeExceptionalError<TContext>(error, prefix, errorName));
    }

    #endregion

    #region Validation<Error, T> Assertions

    internal static void ShouldHaveError<TContext, T>(
        Validation<Error, T> validation, string prefix, string errorName)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";
        var errors = validation.Errors;

        var hasMatchingError = errors.Any(e => e.ErrorCode == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");
    }

    internal static void ShouldHaveOnlyError<TContext, T>(
        Validation<Error, T> validation, string prefix, string errorName)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";
        var errors = validation.Errors;

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors[0];
        var actualErrorCode = singleError.ErrorCode;
        actualErrorCode.ShouldNotBeNull($"Error should be ExpectedError or ExpectedError<T>, but was {singleError.GetType().Name}");
        actualErrorCode.ShouldBe(expectedErrorCode);
    }

    internal static void ShouldHaveErrors<TContext, T>(
        Validation<Error, T> validation, string prefix, params string[] errorNames)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCodes = errorNames
            .Select(name => $"{prefix}.{typeof(TContext).Name}.{name}")
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

    internal static void ShouldHaveError<TContext, T, TValue>(
        Validation<Error, T> validation, string prefix, string errorName,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";
        var errors = validation.Errors;

        var matchingError = errors
            .OfType<ExpectedError<TValue>>()
            .FirstOrDefault(e => e.ErrorCode == expectedErrorCode);

        matchingError.ShouldNotBeNull(
            $"Expected error code '{expectedErrorCode}' with value type '{typeof(TValue).Name}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");

        matchingError!.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
    }

    internal static void ShouldHaveError<TContext, T, T1, T2>(
        Validation<Error, T> validation, string prefix, string errorName,
        T1 expectedValue1, T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";
        var errors = validation.Errors;

        var matchingError = errors
            .OfType<ExpectedError<T1, T2>>()
            .FirstOrDefault(e => e.ErrorCode == expectedErrorCode);

        matchingError.ShouldNotBeNull(
            $"Expected error code '{expectedErrorCode}' with value types '{typeof(T1).Name}, {typeof(T2).Name}' not found. " +
            $"Actual errors: [{string.Join(", ", errors.Select(e => e.Message))}]");

        matchingError!.ErrorCurrentValue1.ShouldBe(expectedValue1);
        matchingError!.ErrorCurrentValue2.ShouldBe(expectedValue2);
    }

    internal static void ShouldHaveError<TContext, T, T1, T2, T3>(
        Validation<Error, T> validation, string prefix, string errorName,
        T1 expectedValue1, T2 expectedValue2, T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{prefix}.{typeof(TContext).Name}.{errorName}";
        var errors = validation.Errors;

        var matchingError = errors
            .OfType<ExpectedError<T1, T2, T3>>()
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
