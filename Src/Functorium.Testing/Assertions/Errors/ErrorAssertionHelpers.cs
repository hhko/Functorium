using System.Linq;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Testing.Assertions.Errors;

/// <summary>
/// 에러 검증을 위한 C# 14 Extension Members
/// </summary>
/// <remarks>
/// Error 및 Validation에 대한 공통 확장 속성을 제공합니다.
/// </remarks>
public static class ErrorAssertionHelpers
{
    /// <summary>
    /// Error에 대한 확장 속성
    /// </summary>
    extension(Error error)
    {
        /// <summary>
        /// Error에서 에러 코드를 추출합니다.
        /// IHasErrorCode 인터페이스를 구현하지 않은 경우 null을 반환합니다.
        /// </summary>
        public string? ErrorCode =>
            error is IHasErrorCode hasErrorCode ? hasErrorCode.ErrorCode : null;

        /// <summary>
        /// Error가 에러 코드를 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasErrorCode => error is IHasErrorCode;
    }

    /// <summary>
    /// Validation&lt;Error, T&gt;에 대한 확장 속성
    /// </summary>
    extension<T>(Validation<Error, T> validation)
    {
        /// <summary>
        /// Validation에서 에러 목록을 추출합니다.
        /// ManyErrors인 경우 모든 에러를, 단일 에러인 경우 해당 에러만 반환합니다.
        /// 성공인 경우 빈 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get
            {
                if (validation.IsSuccess) return [];
                return ((Error)validation).AsIterable().ToList();
            }
        }
    }
}
