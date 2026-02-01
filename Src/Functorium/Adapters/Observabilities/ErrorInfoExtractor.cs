using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Naming;
using LanguageExt.Common;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// Error/Exception에서 관찰 가능성 정보를 추출하는 유틸리티.
/// </summary>
public static class ErrorInfoExtractor
{
    /// <summary>
    /// 에러에서 타입과 코드 정보를 추출합니다.
    /// ManyErrors의 경우 대표 에러를 선정합니다 (Exceptional 우선).
    /// </summary>
    /// <remarks>
    /// 패턴 매칭 순서가 중요합니다:
    /// 1. ManyErrors - 특수 처리 필요
    /// 2. ErrorCodeExceptional - Exceptional 명시적 처리
    /// 3. IHasErrorCode - Expected 에러 (ErrorCodeExceptional도 이 인터페이스를 구현하므로 순서 중요!)
    /// 4. Fallback - 알 수 없는 에러 타입
    /// </remarks>
    public static (string ErrorType, string ErrorCode) GetErrorInfo(Error error)
    {
        return error switch
        {
            // 1순위: ManyErrors - 복합 에러는 특수 처리
            ManyErrors many => (
                ErrorType: ObservabilityNaming.ErrorTypes.Aggregate,
                ErrorCode: GetPrimaryErrorCode(many)
            ),
            // 2순위: ErrorCodeExceptional - Exceptional을 먼저 매칭
            // (IHasErrorCode보다 먼저 와야 함!)
            ErrorCodeExceptional exceptional => (
                ErrorType: ObservabilityNaming.ErrorTypes.Exceptional,
                ErrorCode: exceptional.ErrorCode
            ),
            // 3순위: IHasErrorCode - Expected 에러 (모든 ErrorCodeExpected<...> 변형 포함)
            IHasErrorCode hasErrorCode => (
                ErrorType: ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: hasErrorCode.ErrorCode
            ),
            // 4순위: Fallback - 알 수 없는 에러 타입
            _ => (
                ErrorType: error.IsExceptional
                    ? ObservabilityNaming.ErrorTypes.Exceptional
                    : ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: error.GetType().Name
            )
        };
    }

    /// <summary>
    /// 예외에서 타입과 코드 정보를 추출합니다.
    /// IHasErrorCode 구현체, OperationCanceledException 등을 구분합니다.
    /// </summary>
    public static (string ErrorType, string ErrorCode) GetErrorInfo(Exception exception)
    {
        return exception switch
        {
            IHasErrorCode hasErrorCode => (
                ErrorType: ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: hasErrorCode.ErrorCode
            ),
            OperationCanceledException => (
                ErrorType: ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: "OperationCancelled"
            ),
            _ => (
                ErrorType: ObservabilityNaming.ErrorTypes.Exceptional,
                ErrorCode: exception.GetType().Name
            )
        };
    }

    /// <summary>
    /// ManyErrors에서 대표 에러 코드를 선정합니다.
    /// 우선순위: Exceptional > First > "ManyErrors"
    /// </summary>
    private static string GetPrimaryErrorCode(ManyErrors many)
    {
        // 1순위: Exceptional 에러 (시스템 에러가 더 심각)
        foreach (Error e in many.Errors)
        {
            if (e.IsExceptional)
                return GetErrorCode(e);
        }

        // 2순위: 첫 번째 에러
        return many.Errors.Head.Match(
            Some: GetErrorCode,
            None: () => nameof(ManyErrors));
    }

    /// <summary>
    /// 단일 에러에서 에러 코드를 추출합니다.
    /// </summary>
    private static string GetErrorCode(Error error)
    {
        return error switch
        {
            IHasErrorCode hasErrorCode => hasErrorCode.ErrorCode,
            _ => error.GetType().Name
        };
    }
}
