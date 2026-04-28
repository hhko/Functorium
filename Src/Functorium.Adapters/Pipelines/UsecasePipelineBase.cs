using System.Text.RegularExpressions;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Usecases;

using LanguageExt.Common;

namespace Functorium.Adapters.Pipelines;

// ---------------------------------------
// Logging
// ---------------------------------------
// Request
//  - Information
//      {RequestLayer} {RequestCategory} {RequestCategoryType} {RequestHandler} {RequestHandlerPath} requesting
//
// Response
//  - Information
//      {RequestLayer} {RequestCategory} {RequestCategoryType} {RequestHandler} {RequestHandlerPath} responded {Status} in {Elapsed:0.0000} ms
//  - Warning: ExpectedError
//      {RequestLayer} {RequestCategory} {RequestCategoryType} {RequestHandler} {RequestHandlerPath} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}
//  - Error: ExceptionalError
//      {RequestLayer} {RequestCategory} {RequestCategoryType} {RequestHandler} {RequestHandlerPath} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}
//
// Fields
//  - RequestLayer      : Application, Adapter
//  - RequestCategory       : Usecase, Worker, ...
//  - RequestCategoryType  : Command, Query, Unknown                   <- Usecase일 때만
//  - RequestHandler       : 클래스 이름
//  - RequestHandlerPath       : 네임스페이스를 포함한 전체 클래스 이름
//  - Status            : Succ, Fail
//  - Elapsed           : 0.0000
//  - Error             : ExpectedError, ExpectedError<T1, T2, T3>, ExceptionalError, ManyErrors

// ---------------------------------------
// Tracing
// ---------------------------------------
// Fields
//  - RequestLayer
//  - RequestCategory
//  - RequestCategoryType
//  - RequestHandler
//  - RequestHandlerPath
//  - RequestHandler
//
//  - Status
//  - Elapsed
//    - StartTime
//    - EndTime
//
//                  | ExpectedError | ExceptionalError | ManyErrors | 기본
//  - ErrorKind     | O                 | O                    | O          | O
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

    protected static string GetRequestCategoryType<T>(T request)
    {
        Type[] interfaces = request!.GetType().GetInterfaces();

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandRequest<>)))
            return ObservabilityNaming.CategoryTypes.Command;

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryRequest<>)))
            return ObservabilityNaming.CategoryTypes.Query;

        return ObservabilityNaming.CategoryTypes.Unknown;
    }

    protected static string GetRequestCategoryType(Type requestType)
    {
        Type[] interfaces = requestType.GetInterfaces();

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandRequest<>)))
            return ObservabilityNaming.CategoryTypes.Command;

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryRequest<>)))
            return ObservabilityNaming.CategoryTypes.Query;

        return ObservabilityNaming.CategoryTypes.Unknown;
    }

    // ----------------------------------------
    // Generic 인자(TRequest)별 static 캐시.
    // .NET 런타임은 generic 인자별로 별도 static 영역을 할당하므로
    // ConcurrentDictionary 같은 동적 lookup 자료구조 없이도 자연스럽게
    // 타입별 boundedness가 보장되고 lookup 비용이 0입니다.
    // ----------------------------------------
    private static readonly string _cachedHandlerPath  = typeof(TRequest).FullName ?? string.Empty;
    private static readonly string _cachedHandler      = ComputeRequestHandler();
    private static readonly string _cachedHandlerLower = _cachedHandler.ToLowerInvariant();

    protected static string GetRequestHandlerPath() => _cachedHandlerPath;

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
    protected static string GetRequestHandler() => _cachedHandler;

    /// <summary>
    /// GetRequestHandler()의 소문자 버전.
    /// 메트릭 네이밍 등 소문자가 필요한 경우 사용합니다.
    /// </summary>
    protected static string GetRequestHandlerLower() => _cachedHandlerLower;

    private static string ComputeRequestHandler()
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
    /// </summary>
    protected static (string ErrorType, string ErrorCode) GetErrorInfo(Error error)
        => ErrorInfoExtractor.GetErrorInfo(error);
}
