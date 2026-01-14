using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Functorium.Abstractions.Errors;

[DataContract]
internal record ErrorCodeExceptional : Error, IHasErrorCode
{
    [Pure]
    [DataMember]
    public string ErrorCode { get; init; }

    private readonly Option<Error> _inner;

    public ErrorCodeExceptional(string errorCode, Exception exception)
    {
        ErrorCode = errorCode;
        Value = exception;

        Message = exception.Message;
        Code = exception.HResult;

        _inner = exception.InnerException == null
            ? None
            : New(exception.InnerException);
    }

    [IgnoreDataMember]
    readonly Exception? Value;

    [DataMember]
    public override string Message { get; }

    [DataMember]
    public override int Code { get; }

    public override string ToString() =>
        Message;

    [Pure]
    public override Option<Error> Inner =>
        _inner;

    [Pure]
    public override Exception ToException() =>
        Value ?? new ExceptionalException(Message, Code);

    [Pure]
    public override ErrorException ToErrorException() =>
        Value == null
            ? new WrappedErrorExceptionalException(this)
            : new ExceptionalException(Value);

    public override R Throw<R>() =>
        Value is null
            ? throw ToErrorException()
            : Value.Rethrow<R>();

    [Pure]
    public override bool HasException<E>() =>
        Value is E;

    [Pure]
    public override bool Is(Error error) =>
        error is ManyErrors errors
            ? errors.Errors.Exists(Is)
            : Value == null
                ? error.IsExceptional && Code == error.Code && Message == error.Message
                : error.IsExceptional && Value.GetType().IsInstanceOfType(error.ToException());

    [Pure]
    [IgnoreDataMember]
    public override bool IsExceptional =>
        true;

    [Pure]
    [IgnoreDataMember]
    public override bool IsExpected =>
        false;
}
