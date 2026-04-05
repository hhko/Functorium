using Functorium.Abstractions.Errors;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 도메인 에러 검증을 위한 타입 안전 확장 메서드
/// </summary>
public static class DomainErrorAssertions
{
    #region Error Assertions

    public static void ShouldBeDomainError<TDomain>(
        this Error error,
        DomainErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldBeError<TDomain>(error, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName);

    public static void ShouldBeDomainError<TDomain, TValue>(
        this Error error,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldBeError<TDomain, TValue>(error, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedCurrentValue);

    public static void ShouldBeDomainError<TDomain, T1, T2>(
        this Error error,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull =>
        ErrorAssertionCore.ShouldBeError<TDomain, T1, T2>(error, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedValue1, expectedValue2);

    public static void ShouldBeDomainError<TDomain, T1, T2, T3>(
        this Error error,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorAssertionCore.ShouldBeError<TDomain, T1, T2, T3>(error, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedValue1, expectedValue2, expectedValue3);

    #endregion

    #region Fin<T> Assertions

    public static void ShouldBeDomainError<TDomain, T>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldBeFinError<TDomain, T>(fin, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName);

    public static void ShouldBeDomainError<TDomain, T, TValue>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldBeFinError<TDomain, T, TValue>(fin, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedCurrentValue);

    #endregion

    #region Validation<Error, T> Assertions

    public static void ShouldHaveDomainError<TDomain, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldHaveError<TDomain, T>(validation, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName);

    public static void ShouldHaveOnlyDomainError<TDomain, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldHaveOnlyError<TDomain, T>(validation, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName);

    public static void ShouldHaveDomainErrors<TDomain, T>(
        this Validation<Error, T> validation,
        params DomainErrorType[] expectedErrorTypes) =>
        ErrorAssertionCore.ShouldHaveErrors<TDomain, T>(
            validation, ErrorType.DomainErrorsPrefix,
            expectedErrorTypes.Select(et => et.ErrorName).ToArray());

    public static void ShouldHaveDomainError<TDomain, T, TValue>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldHaveError<TDomain, T, TValue>(validation, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedCurrentValue);

    public static void ShouldHaveDomainError<TDomain, T, T1, T2>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull =>
        ErrorAssertionCore.ShouldHaveError<TDomain, T, T1, T2>(validation, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedValue1, expectedValue2);

    public static void ShouldHaveDomainError<TDomain, T, T1, T2, T3>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorAssertionCore.ShouldHaveError<TDomain, T, T1, T2, T3>(validation, ErrorType.DomainErrorsPrefix, expectedErrorType.ErrorName, expectedValue1, expectedValue2, expectedValue3);

    #endregion
}
