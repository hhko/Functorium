using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Functorium.Abstractions.Errors;

[DataContract]
internal record ErrorCodeExpected(
    string ErrorCode,
    string ErrorCurrentValue,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : ErrorCodeExpectedBase(ErrorCode, ErrorMessage, ErrorCodeId, Inner)
{
    [Pure]
    [DataMember]
    public string ErrorCurrentValue { get; } =
        ErrorCurrentValue;
}

[DataContract]
internal record ErrorCodeExpected<T>(
    string ErrorCode,
    T ErrorCurrentValue,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : ErrorCodeExpectedBase(ErrorCode, ErrorMessage, ErrorCodeId, Inner)
        where T : notnull
{
    [Pure]
    [DataMember]
    public T ErrorCurrentValue { get; } =
        ErrorCurrentValue;
}

[DataContract]
internal record ErrorCodeExpected<T1, T2>(
    string ErrorCode,
    T1 ErrorCurrentValue1,
    T2 ErrorCurrentValue2,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : ErrorCodeExpectedBase(ErrorCode, ErrorMessage, ErrorCodeId, Inner)
        where T1 : notnull
        where T2 : notnull
{
    [Pure]
    [DataMember]
    public T1 ErrorCurrentValue1 { get; } =
        ErrorCurrentValue1;

    [Pure]
    [DataMember]
    public T2 ErrorCurrentValue2 { get; } =
        ErrorCurrentValue2;
}

[DataContract]
internal record ErrorCodeExpected<T1, T2, T3>(
    string ErrorCode,
    T1 ErrorCurrentValue1,
    T2 ErrorCurrentValue2,
    T3 ErrorCurrentValue3,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : ErrorCodeExpectedBase(ErrorCode, ErrorMessage, ErrorCodeId, Inner)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
{
    [Pure]
    [DataMember]
    public T1 ErrorCurrentValue1 { get; } =
        ErrorCurrentValue1;

    [Pure]
    [DataMember]
    public T2 ErrorCurrentValue2 { get; } =
        ErrorCurrentValue2;

    [Pure]
    [DataMember]
    public T3 ErrorCurrentValue3 { get; } =
        ErrorCurrentValue3;
}
