using System.Text.RegularExpressions;

using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Cqrs;

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
//  - ErrorCode     | O                 | O                    | X          | X
//  - ErrorMessage  | O                 | O                    | X          | O
//  - ErrorCount    | X                 | X                    | O          | X

public abstract class UsecasePipelineBase<TRequest>
{
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
        Match plusMatch = Regex.Match(input, @"\.([^.+]+)\+");
        if (plusMatch.Success)
        {
            return plusMatch.Groups[1].Value;
        }

        // "+"가 있으나 "."이 없는 경우: "^([^+]+)\+"
        Match beforePlusMatch = Regex.Match(input, @"^([^+]+)\+");
        if (beforePlusMatch.Success)
        {
            return beforePlusMatch.Groups[1].Value;
        }

        // "+"가 없는 경우: ".xxx$"
        Match afterLastDotMatch = Regex.Match(input, @"\.([^.+]+)$");
        if (afterLastDotMatch.Success)
        {
            return afterLastDotMatch.Groups[1].Value;
        }

        return string.Empty;
    }
}
