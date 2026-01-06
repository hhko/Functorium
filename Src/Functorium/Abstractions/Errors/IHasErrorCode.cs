namespace Functorium.Abstractions.Errors;

/// <summary>
/// Represents an error that exposes an ErrorCode property for observability purposes.
/// This interface enables type-safe ErrorCode access without reflection.
/// </summary>
internal interface IHasErrorCode
{
    /// <summary>
    /// Gets the error code that uniquely identifies this error type.
    /// </summary>
    string ErrorCode { get; }
}
