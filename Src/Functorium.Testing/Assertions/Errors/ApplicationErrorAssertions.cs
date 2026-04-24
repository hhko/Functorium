using Functorium.Abstractions.Errors;
using Functorium.Applications.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 애플리케이션 에러 검증을 위한 타입 안전 확장 메서드
/// </summary>
public static class ApplicationErrorAssertions
{
    #region Error Assertions

    public static void ShouldBeApplicationError<TUsecase>(
        this Error error,
        ApplicationErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldBeError<TUsecase>(error, ErrorCodePrefixes.Application, expectedErrorType.ErrorName);

    public static void ShouldBeApplicationError<TUsecase, TValue>(
        this Error error,
        ApplicationErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldBeError<TUsecase, TValue>(error, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedCurrentValue);

    public static void ShouldBeApplicationError<TUsecase, T1, T2>(
        this Error error,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull =>
        ErrorAssertionCore.ShouldBeError<TUsecase, T1, T2>(error, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedValue1, expectedValue2);

    public static void ShouldBeApplicationError<TUsecase, T1, T2, T3>(
        this Error error,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorAssertionCore.ShouldBeError<TUsecase, T1, T2, T3>(error, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedValue1, expectedValue2, expectedValue3);

    #endregion

    #region Fin<T> Assertions

    public static void ShouldBeApplicationError<TUsecase, T>(
        this Fin<T> fin,
        ApplicationErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldBeFinError<TUsecase, T>(fin, ErrorCodePrefixes.Application, expectedErrorType.ErrorName);

    public static void ShouldBeApplicationError<TUsecase, T, TValue>(
        this Fin<T> fin,
        ApplicationErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldBeFinError<TUsecase, T, TValue>(fin, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedCurrentValue);

    #endregion

    #region Validation<Error, T> Assertions

    public static void ShouldHaveApplicationError<TUsecase, T>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldHaveError<TUsecase, T>(validation, ErrorCodePrefixes.Application, expectedErrorType.ErrorName);

    public static void ShouldHaveOnlyApplicationError<TUsecase, T>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType) =>
        ErrorAssertionCore.ShouldHaveOnlyError<TUsecase, T>(validation, ErrorCodePrefixes.Application, expectedErrorType.ErrorName);

    public static void ShouldHaveApplicationErrors<TUsecase, T>(
        this Validation<Error, T> validation,
        params ApplicationErrorType[] expectedErrorTypes) =>
        ErrorAssertionCore.ShouldHaveErrors<TUsecase, T>(
            validation, ErrorCodePrefixes.Application,
            expectedErrorTypes.Select(et => et.ErrorName).ToArray());

    public static void ShouldHaveApplicationError<TUsecase, T, TValue>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldHaveError<TUsecase, T, TValue>(validation, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedCurrentValue);

    public static void ShouldHaveApplicationError<TUsecase, T, T1, T2>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull =>
        ErrorAssertionCore.ShouldHaveError<TUsecase, T, T1, T2>(validation, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedValue1, expectedValue2);

    public static void ShouldHaveApplicationError<TUsecase, T, T1, T2, T3>(
        this Validation<Error, T> validation,
        ApplicationErrorType expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorAssertionCore.ShouldHaveError<TUsecase, T, T1, T2, T3>(validation, ErrorCodePrefixes.Application, expectedErrorType.ErrorName, expectedValue1, expectedValue2, expectedValue3);

    #endregion
}
