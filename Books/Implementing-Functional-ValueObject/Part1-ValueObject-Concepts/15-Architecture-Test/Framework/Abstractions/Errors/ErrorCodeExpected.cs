using LanguageExt;
using LanguageExt.Common;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Framework.Abstractions.Errors;

[DataContract]
internal record ErrorCodeExpected(
    string ErrorCode,
    string ErrorCurrentValue,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default) : Error
{
    //
    // 새 Error 코드
    //

    [Pure]
    [DataMember]
    public string ErrorCode { get; } =
        ErrorCode;

    [Pure]
    [DataMember]
    public string ErrorCurrentValue { get; } =
        ErrorCurrentValue;

    //
    // 기존 Error 코드
    //

    [Pure]
    [DataMember]
    public override string Message { get; } =
        //ErrorMessage;
        string.Empty;

    [Pure]
    [DataMember]
    public override int Code { get; } =
        ErrorCodeId;

    [Pure]
    [IgnoreDataMember]
    public override Option<Error> Inner { get; } =
        Inner;

    [Pure]
    public override string ToString() =>
        Message;

    [Pure]
    public override ErrorException ToErrorException() =>
        new WrappedErrorExpectedException(this);

    public override R Throw<R>() =>
        throw ToErrorException();

    [Pure]
    public override bool HasException<E>() =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExceptional =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExpected =>
        true;
}

[DataContract]
internal record ErrorCodeExpected<T>(
    string ErrorCode,
    T ErrorCurrentValue,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : Error where T : notnull
{
    //
    // 새 Error 코드
    //

    [Pure]
    [DataMember]
    public string ErrorCode { get; } =
        ErrorCode;

    [Pure]
    [DataMember]
    public T ErrorCurrentValue { get; } =
        ErrorCurrentValue;

    //
    // 기존 Error 코드
    //

    [Pure]
    [DataMember]
    public override string Message { get; } =
        //ErrorMessage;
        string.Empty;

    [Pure]
    [DataMember]
    public override int Code { get; } =
        ErrorCodeId;

    [Pure]
    [IgnoreDataMember]
    public override Option<Error> Inner { get; } =
        Inner;

    [Pure]
    public override string ToString() =>
        Message;

    [Pure]
    public override ErrorException ToErrorException() =>
        new WrappedErrorExpectedException(this);

    public override R Throw<R>() =>
        throw ToErrorException();

    [Pure]
    public override bool HasException<E>() =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExceptional =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExpected =>
        true;
}

[DataContract]
internal record ErrorCodeExpected<T1, T2>(
    string ErrorCode,
    T1 ErrorCurrentValue1,
    T2 ErrorCurrentValue2,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : Error
        where T1 : notnull
        where T2 : notnull
{
    //
    // 새 Error 코드
    //

    [Pure]
    [DataMember]
    public string ErrorCode { get; } =
        ErrorCode;

    [Pure]
    [DataMember]
    public T1 ErrorCurrentValue1 { get; } =
        ErrorCurrentValue1;

    [Pure]
    [DataMember]
    public T2 ErrorCurrentValue2 { get; } =
        ErrorCurrentValue2;

    //
    // 기존 Error 코드
    //

    [Pure]
    [DataMember]
    public override string Message { get; } =
        //ErrorMessage;
        string.Empty;

    [Pure]
    [DataMember]
    public override int Code { get; } =
        ErrorCodeId;

    [Pure]
    [IgnoreDataMember]
    public override Option<Error> Inner { get; } =
        Inner;

    [Pure]
    public override string ToString() =>
        Message;

    [Pure]
    public override ErrorException ToErrorException() =>
        new WrappedErrorExpectedException(this);

    public override R Throw<R>() =>
        throw ToErrorException();

    [Pure]
    public override bool HasException<E>() =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExceptional =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExpected =>
        true;
}

[DataContract]
internal record ErrorCodeExpected<T1, T2, T3>(
    string ErrorCode,
    T1 ErrorCurrentValue1,
    T2 ErrorCurrentValue2,
    T3 ErrorCurrentValue3,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : Error
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
{
    //
    // 새 Error 코드
    //

    [Pure]
    [DataMember]
    public string ErrorCode { get; } =
        ErrorCode;

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

    //
    // 기존 Error 코드
    //

    [Pure]
    [DataMember]
    public override string Message { get; } =
        //ErrorMessage;
        string.Empty;

    [Pure]
    [DataMember]
    public override int Code { get; } =
        ErrorCodeId;

    [Pure]
    [IgnoreDataMember]
    public override Option<Error> Inner { get; } =
        Inner;

    [Pure]
    public override string ToString() =>
        Message;

    [Pure]
    public override ErrorException ToErrorException() =>
        new WrappedErrorExpectedException(this);

    public override R Throw<R>() =>
        throw ToErrorException();

    [Pure]
    public override bool HasException<E>() =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExceptional =>
        false;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExpected =>
        true;
}
