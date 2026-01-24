using Functorium.Abstractions.Errors;
using LanguageExt.Common;

namespace Functorium.Applications.Validations;

/// <summary>
/// FluentValidation과 값 객체 Validate 메서드를 통합하기 위한 확장 메서드
/// </summary>
public static partial class FluentValidationExtensions
{
    internal static string FormatErrorMessage(Error error) =>
        error is IHasErrorCode hasErrorCode
            ? $"[{hasErrorCode.ErrorCode}] {error.Message}"
            : error.Message;
}
