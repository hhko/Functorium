using LanguageExt.Common;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Framework.Abstractions.Errors;

public static class ErrorCodeFactory
{
    // ErrorCodeExpected
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create(string errorCode,
                               string errorCurrentValue,
                               string errorMessage) =>
        new ErrorCodeExpected(
            errorCode,
            errorCurrentValue,
            errorMessage);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create(string errorCode,
                               int errorCurrentValue,
                               string errorMessage) =>
        new ErrorCodeExpected(
            errorCode,
            errorCurrentValue.ToString(),
            errorMessage);

    // ErrorCodeExpected<T>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T>(string errorCode,
                                  T errorCurrentValue,
                                  string errorMessage) where T : notnull =>
        new ErrorCodeExpected<T>(
            errorCode,
            errorCurrentValue,
            errorMessage);

    // ErrorCodeExpected<T1, T2>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2>(string errorCode,
                                       T1 errorCurrentValue1,
                                       T2 errorCurrentValue2,
                                       string errorMessage) where T1 : notnull where T2 : notnull =>
        new ErrorCodeExpected<T1, T2>(
            errorCode,
            errorCurrentValue1,
            errorCurrentValue2,
            errorMessage);

    // ErrorCodeExpected<T1, T2, T3>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2, T3>(string errorCode,
                                           T1 errorCurrentValue1,
                                           T2 errorCurrentValue2,
                                           T3 errorCurrentValue3,
                                           string errorMessage) where T1 : notnull where T2 : notnull where T3 : notnull =>
        new ErrorCodeExpected<T1, T2, T3>(
            errorCode,
            errorCurrentValue1,
            errorCurrentValue2,
            errorCurrentValue3,
            errorMessage);

    // ErrorCodeExceptional
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateFromException(string errorCode,
                                            Exception exception) =>
        new ErrorCodeExceptional(
            errorCode,
            exception);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(params string[] parts) =>
        string.Join('.', parts);
}

