namespace Framework.Abstractions.Errors;

internal static class ErrorCodeFieldNames
{
    public const string ErrorCode = nameof(ErrorCodeExpected.ErrorCode);
    public const string Message = nameof(ErrorCodeExpected.Message);
    public const string ErrorCurrentValue = nameof(ErrorCodeExpected.ErrorCurrentValue);
    public const string ErrorCodeId = nameof(ErrorCodeExpected.ErrorCodeId);

    public const string ErrorReason = nameof(ErrorReason);
    public const string Count = nameof(Count);
    public const string Errors = nameof(Errors);
    public const string InnerError = nameof(InnerError);
    public const string ExceptionDetails = nameof(ExceptionDetails);

    public const string UnkownErrorCode = "UNKNOWN.ERROR-CODE";
    public const string UnkownErrorMessage = "UNKNOWN.ERROR-MESSAGE";
}
