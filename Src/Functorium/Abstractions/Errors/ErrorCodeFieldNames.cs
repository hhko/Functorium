namespace Functorium.Abstractions.Errors;

internal static class ErrorCodeFieldNames
{
    public const string ErrorCode = nameof(ExpectedError.ErrorCode);
    public const string Message = nameof(ExpectedError.Message);
    public const string ErrorCurrentValue = nameof(ExpectedError.ErrorCurrentValue);
    public const string ErrorCodeId = nameof(ExpectedError.ErrorCodeId);

    public const string ErrorType = nameof(ErrorType);
    public const string Count = nameof(Count);
    public const string Errors = nameof(Errors);
    public const string InnerError = nameof(InnerError);
    public const string ExceptionDetails = nameof(ExceptionDetails);

    public const string UnknownErrorCode = "UNKNOWN.ERROR-CODE";
    public const string UnknownErrorMessage = "UNKNOWN.ERROR-MESSAGE";
}
