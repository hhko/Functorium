using System.Text.RegularExpressions;

using Functorium.Applications.Cqrs;
using Functorium.Applications.Observabilities;

namespace Functorium.Applications.Pipelines;

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


    ///// <summary>
    ///// Trace 전용: Stopwatch 타임스탬프를 DateTimeOffset으로 변환합니다.
    ///// </summary>
    //public static DateTimeOffset GetDateTimeFromTimestamp(long timestamp)
    //{
    //    if (timestamp == 0)
    //        return DateTimeOffset.MinValue;

    //    //
    //    // IMPORTANCE: long 타입 오버플로우 방지를 위해 double로 계산
    //    //
    //    var ticks = (long)(timestamp * (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency);
    //    return DateTimeOffset.FromFileTime(ticks);
    //}


    protected static string GetRequestHandlerPath()
    {
        return typeof(TRequest).FullName!; //$"{typeof(TRequest).Namespace}.{typeof(TRequest).Name}";
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

///// <summary>
///// MediatR Request 객체에 추가 데이터를 저장하기 위한 확장 메서드들
///// ConditionalWeakTable을 사용하여 메모리 누수를 방지하고 스레드 안전하게 데이터를 관리합니다.
/////
///// 메모리 관리 원리:
///// - ConditionalWeakTable은 키(Request 객체)를 WeakReference로 관리
///// - Request 객체가 GC되면 자동으로 테이블 엔트리도 제거됨
///// - 따라서 장시간 운영해도 메모리가 지속적으로 증가하지 않음
///// - ASP.NET Core의 HttpContext.Items도 내부적으로 유사한 메커니즘 사용
///// </summary>
//internal static class RequestExtensions
//{
//    /// <summary>
//    /// Request 객체와 elapsedMs를 연결하여 저장하는 ConditionalWeakTable
//    /// 키(Request 객체)가 GC되면 값도 자동으로 정리되어 메모리 누수 방지
//    /// </summary>
//    private static readonly ConditionalWeakTable<object, object> _elapsedTimes = new();

//    /// <summary>
//    /// Request 객체에 elapsedMs를 저장합니다.
//    /// 기존 값이 있으면 덮어씁니다.
//    /// </summary>
//    /// <param name="request">MediatR Request 객체</param>
//    /// <param name="elapsedMs">측정된 경과 시간 (밀리초)</param>
//    public static void SetElapsedMs(this object request, double elapsedMs)
//    {
//        _elapsedTimes.AddOrUpdate(request, elapsedMs);
//    }

//    /// <summary>
//    /// Request 객체에서 elapsedMs를 읽어옵니다.
//    /// 저장된 값이 없으면 0을 반환합니다.
//    /// </summary>
//    /// <param name="request">MediatR Request 객체</param>
//    /// <returns>저장된 elapsedMs 값 또는 0</returns>
//    public static double GetElapsedMs(this object request)
//    {
//        return _elapsedTimes.TryGetValue(request, out var elapsedMs) ? (double)elapsedMs : 0;
//    }
//}
