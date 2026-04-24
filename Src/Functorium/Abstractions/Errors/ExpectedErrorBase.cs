using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Functorium.Abstractions.Errors;

/// <summary>
/// ExpectedError 계열 record의 공통 Error override를 제공하는 기반 클래스
/// </summary>
[DataContract]
internal abstract record ExpectedErrorBase(
    string ErrorCode,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default) : Error, IHasErrorCode
{
    [Pure]
    [DataMember]
    public string ErrorCode { get; } =
        ErrorCode;

    [Pure]
    [DataMember]
    public override string Message { get; } =
        ErrorMessage;

    [Pure]
    [DataMember]
    public override int Code { get; } =
        ErrorCodeId;

    [Pure]
    [IgnoreDataMember]
    public override Option<Error> Inner { get; } =
        Inner;

    [Pure]
    public sealed override string ToString() =>
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
