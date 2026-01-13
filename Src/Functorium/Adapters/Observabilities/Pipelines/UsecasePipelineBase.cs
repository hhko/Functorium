using System.Text.RegularExpressions;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

namespace Functorium.Adapters.Observabilities.Pipelines;

// ---------------------------------------
// Logging
// ---------------------------------------
// Request
//  - Information
//      {RequestLayer} {RequestCategory} {RequestHandlerCqrs} {RequestHandler} {RequestHandlerPath} requesting
//
// Response
//  - Information
//      {RequestLayer} {RequestCategory} {RequestHandlerCqrs} {RequestHandler} {RequestHandlerPath} responded {Status} in {Elapsed:0.0000} ms
//  - Warning: ErrorCodeExpected
//      {RequestLayer} {RequestCategory} {RequestHandlerCqrs} {RequestHandler} {RequestHandlerPath} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}
//  - Error: ErrorCodeExceptional
//      {RequestLayer} {RequestCategory} {RequestHandlerCqrs} {RequestHandler} {RequestHandlerPath} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}
//
// Fields
//  - RequestLayer      : Application, Adapter
//  - RequestCategory       : Usecase, Worker, ...
//  - RequestHandlerCqrs  : Command, Query, Unknown                   <- Usecase일 때만
//  - RequestHandler       : 클래스 이름
//  - RequestHandlerPath       : 네임스페이스를 포함한 전체 클래스 이름
//  - Status            : Succ, Fail
//  - Elapsed           : 0.0000
//  - Error             : ErrorCodeExpected, ErrorCodeExpected<T1, T2, T3>, ErrorCodeExceptional, ManyErrors

// ---------------------------------------
// Tracing
// ---------------------------------------
// Fields
//  - RequestLayer
//  - RequestCategory
//  - RequestHandlerCqrs
//  - RequestHandler
//  - RequestHandlerPath
//  - RequestHandler
//
//  - Status
//  - Elapsed
//    - StartTime
//    - EndTime
//
//                  | ErrorCodeExpected | ErrorCodeExceptional | ManyErrors | 기본
//  - ErrorType     | O                 | O                    | O          | O
//  - ErrorCode     | O                 | O                    | O          | X

public abstract partial class UsecasePipelineBase<TRequest>
{
    // GeneratedRegex for AOT-compiled regex patterns to eliminate runtime compilation overhead
    [GeneratedRegex(@"\.([^.+]+)\+", RegexOptions.Compiled)]
    private static partial Regex PlusPattern();

    [GeneratedRegex(@"^([^+]+)\+", RegexOptions.Compiled)]
    private static partial Regex BeforePlusPattern();

    [GeneratedRegex(@"\.([^.+]+)$", RegexOptions.Compiled)]
    private static partial Regex AfterLastDotPattern();

    protected static string GetRequestCqrs<T>(T request)
    {
        Type[] interfaces = request!.GetType().GetInterfaces();

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandRequest<>)))
            return ObservabilityNaming.Cqrs.Command;

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryRequest<>)))
            return ObservabilityNaming.Cqrs.Query;

        return ObservabilityNaming.Cqrs.Unknown;
    }

    protected static string GetRequestCqrs(Type requestType)
    {
        Type[] interfaces = requestType.GetInterfaces();

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandRequest<>)))
            return ObservabilityNaming.Cqrs.Command;

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryRequest<>)))
            return ObservabilityNaming.Cqrs.Query;

        return ObservabilityNaming.Cqrs.Unknown;
    }

    protected static string GetRequestHandlerPath()
    {
        return typeof(TRequest).FullName!;
    }

    // - "+"가 존재할 경우
    //   - 가장 첫 번째 "+" 기준으로 처리
    //   - 그 앞에 "."이 존재하면: 해당 "."과 "+" 사이의 문자열을 추출
    //   - "."이 존재하지 않으면: "+" 앞의 전체 문자열을 추출
    //
    //   "+" 앞에 가장 가까운 "."이 존재하면, 그 사이 문자열 추출 ("."과 "+" 사이)
    //   - "."이 없다면, "+" 앞 문자열 전체 추출
    // - "+"가 존재하지 않을 경우
    //   - 마지막 "." 기준으로 그 이후의 문자열을 추출

    // 입력 문자열               | 출력          | 설명
    // -----------------------  | ---------    | ----------------------------
    // `mail.site+abc+def.com`  | `site`       | 첫 번째 `"+"` 기준, 그 앞 마지막 `"."`
    // `a.b.c+xyz+foo.bar`      | `c`          | 첫 번째 `"+"` 기준, 그 앞 마지막 `"."`
    // `invalid+test+more`      | `invalid`    | `"."` 없음 → `"+"` 앞 문자열 전체
    // `domain.name`            | `name`       | `"+"` 없음 → 마지막 `"."` 이후
    // `no.plus.or.dot`         | `dot`        | `"+"` 없음 → 마지막 `"."` 이후
    // `no_dot_or_plus`         | (빈 문자열)   | `"."`, `"+"` 모두 없음
    protected static string GetRequestHandler()
    {
        string input = typeof(TRequest).FullName!;

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // "+"가 있는 경우: ".xxx+"
        Match plusMatch = PlusPattern().Match(input);
        if (plusMatch.Success)
        {
            return plusMatch.Groups[1].Value;
        }

        // "+"가 있으나 "."이 없는 경우: "^([^+]+)\+"
        Match beforePlusMatch = BeforePlusPattern().Match(input);
        if (beforePlusMatch.Success)
        {
            return beforePlusMatch.Groups[1].Value;
        }

        // "+"가 없는 경우: ".xxx$"
        Match afterLastDotMatch = AfterLastDotPattern().Match(input);
        if (afterLastDotMatch.Success)
        {
            return afterLastDotMatch.Groups[1].Value;
        }

        return string.Empty;
    }

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
    protected static (string ErrorType, string ErrorCode) GetErrorInfo(Error error)
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
