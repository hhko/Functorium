using Functorium.Abstractions.Errors;
using Functorium.Adapters.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 어댑터 에러 검증을 위한 타입 안전 확장 메서드
/// </summary>
public static class AdapterErrorAssertions
{
    #region Error Assertions

    public static void ShouldBeAdapterError<TAdapter>(
        this Error error,
        AdapterErrorKind expectedErrorType) =>
        ErrorAssertionCore.ShouldBeError<TAdapter>(error, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    public static void ShouldBeAdapterError<TAdapter, TValue>(
        this Error error,
        AdapterErrorKind expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldBeError<TAdapter, TValue>(error, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedCurrentValue);

    public static void ShouldBeAdapterError<TAdapter, T1, T2>(
        this Error error,
        AdapterErrorKind expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull =>
        ErrorAssertionCore.ShouldBeError<TAdapter, T1, T2>(error, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedValue1, expectedValue2);

    public static void ShouldBeAdapterError<TAdapter, T1, T2, T3>(
        this Error error,
        AdapterErrorKind expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorAssertionCore.ShouldBeError<TAdapter, T1, T2, T3>(error, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedValue1, expectedValue2, expectedValue3);

    public static void ShouldBeAdapterExceptionalError<TAdapter>(
        this Error error,
        AdapterErrorKind expectedErrorType) =>
        ErrorAssertionCore.ShouldBeExceptionalError<TAdapter>(error, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    public static void ShouldBeAdapterExceptionalError<TAdapter, TException>(
        this Error error,
        AdapterErrorKind expectedErrorType)
        where TException : Exception =>
        ErrorAssertionCore.ShouldBeExceptionalError<TAdapter, TException>(error, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    #endregion

    #region Fin<T> Assertions

    public static void ShouldBeAdapterError<TAdapter, T>(
        this Fin<T> fin,
        AdapterErrorKind expectedErrorType) =>
        ErrorAssertionCore.ShouldBeFinError<TAdapter, T>(fin, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    public static void ShouldBeAdapterError<TAdapter, T, TValue>(
        this Fin<T> fin,
        AdapterErrorKind expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldBeFinError<TAdapter, T, TValue>(fin, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedCurrentValue);

    public static void ShouldBeAdapterExceptionalError<TAdapter, T>(
        this Fin<T> fin,
        AdapterErrorKind expectedErrorType) =>
        ErrorAssertionCore.ShouldBeFinExceptionalError<TAdapter, T>(fin, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    #endregion

    #region Validation<Error, T> Assertions

    public static void ShouldHaveAdapterError<TAdapter, T>(
        this Validation<Error, T> validation,
        AdapterErrorKind expectedErrorType) =>
        ErrorAssertionCore.ShouldHaveError<TAdapter, T>(validation, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    public static void ShouldHaveOnlyAdapterError<TAdapter, T>(
        this Validation<Error, T> validation,
        AdapterErrorKind expectedErrorType) =>
        ErrorAssertionCore.ShouldHaveOnlyError<TAdapter, T>(validation, ErrorCodePrefixes.Adapter, expectedErrorType.Name);

    public static void ShouldHaveAdapterErrors<TAdapter, T>(
        this Validation<Error, T> validation,
        params AdapterErrorKind[] expectedErrorTypes) =>
        ErrorAssertionCore.ShouldHaveErrors<TAdapter, T>(
            validation, ErrorCodePrefixes.Adapter,
            expectedErrorTypes.Select(et => et.Name).ToArray());

    public static void ShouldHaveAdapterError<TAdapter, T, TValue>(
        this Validation<Error, T> validation,
        AdapterErrorKind expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull =>
        ErrorAssertionCore.ShouldHaveError<TAdapter, T, TValue>(validation, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedCurrentValue);

    public static void ShouldHaveAdapterError<TAdapter, T, T1, T2>(
        this Validation<Error, T> validation,
        AdapterErrorKind expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2)
        where T1 : notnull
        where T2 : notnull =>
        ErrorAssertionCore.ShouldHaveError<TAdapter, T, T1, T2>(validation, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedValue1, expectedValue2);

    public static void ShouldHaveAdapterError<TAdapter, T, T1, T2, T3>(
        this Validation<Error, T> validation,
        AdapterErrorKind expectedErrorType,
        T1 expectedValue1,
        T2 expectedValue2,
        T3 expectedValue3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        ErrorAssertionCore.ShouldHaveError<TAdapter, T, T1, T2, T3>(validation, ErrorCodePrefixes.Adapter, expectedErrorType.Name, expectedValue1, expectedValue2, expectedValue3);

    #endregion
}
