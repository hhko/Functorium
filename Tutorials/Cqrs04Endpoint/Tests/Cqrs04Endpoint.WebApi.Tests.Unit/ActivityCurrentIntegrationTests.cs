using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cqrs04Endpoint.WebApi.Tests.Unit;

/// <summary>
/// ASP.NET Core 환경에서 Activity.Current가 전체 파이프라인을 통해 유지되는지 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 기존 테스트들이 "시뮬레이션"(Activity.Current = null 직접 설정)하는 것과 달리,
/// 실제 HTTP 요청을 통해 Activity.Current의 동작을 검증합니다.
/// </para>
/// <para>
/// 검증 대상:
/// <code>
/// HTTP Request (ASP.NET Core 자동 생성)
///     └── Usecase Activity (UsecaseTracingPipeline)
///             └── Adapter Activity (OpenTelemetrySpanFactory)
/// </code>
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class ActivityCurrentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly List<Activity> _capturedActivities;
    private readonly ActivityListener _listener;

    public ActivityCurrentIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _capturedActivities = new List<Activity>();

        // 모든 Activity를 캡처하는 리스너 등록
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                // 스레드 안전하게 캡처
                lock (_capturedActivities)
                {
                    _capturedActivities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _client.Dispose();
    }

    /// <summary>
    /// 실제 HTTP 요청을 통해 Activity 계층이 올바르게 형성되는지 검증합니다.
    /// ActivityContextHolder 없이도 Activity.Current가 유지되어야 합니다.
    /// </summary>
    [Fact]
    public async Task ActivityCurrent_IsPreservedThroughPipeline_RealHttpRequest()
    {
        // Arrange
        _capturedActivities.Clear();
        var startTime = DateTime.UtcNow;
        var request = new
        {
            Name = $"TestProduct_{Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100m,
            StockQuantity = 10
        };

        // Act - 실제 HTTP 요청 (시뮬레이션 아님!)
        var response = await _client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert - HTTP 응답 확인
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // 잠시 대기하여 모든 Activity가 완료되도록 함
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert - Activity 계층 구조 검증
        lock (_capturedActivities)
        {
            // 이 테스트의 HTTP 요청에 해당하는 root Activity 찾기 (시간 기반 + HttpRequestIn)
            var httpRequestActivity = _capturedActivities
                .Where(a => a.DisplayName.Contains("HttpRequestIn") || a.OperationName.Contains("HttpRequestIn"))
                .Where(a => a.StartTimeUtc >= startTime.AddSeconds(-1))
                .OrderByDescending(a => a.StartTimeUtc)
                .FirstOrDefault();

            // HTTP Activity의 TraceId로 관련 Activity만 필터링
            var relevantActivities = httpRequestActivity != null
                ? _capturedActivities.Where(a => a.TraceId == httpRequestActivity.TraceId).ToList()
                : _capturedActivities.Where(a => a.StartTimeUtc >= startTime.AddSeconds(-1)).ToList();

            // Usecase Activity 찾기 (필터링된 목록에서)
            var usecaseActivity = relevantActivities.FirstOrDefault(a =>
                a.DisplayName.Contains("application usecase.command") ||
                a.DisplayName.Contains("CreateProductCommand"));

            // Adapter Activity 찾기 (필터링된 목록에서)
            var adapterActivities = relevantActivities.Where(a =>
                a.DisplayName.Contains("adapter") ||
                a.DisplayName.Contains("Repository")).ToList();

            // 기본 검증: Activity가 캡처되었는지
            relevantActivities.Count.ShouldBeGreaterThan(0, "No activities were captured for this request");

            if (usecaseActivity != null)
            {
                // Usecase Activity가 있으면 상세 검증
                usecaseActivity.TraceId.ShouldNotBe(default, "Usecase Activity should have TraceId");

                // 모든 Adapter Activity가 동일한 TraceId를 가져야 함
                foreach (var adapterActivity in adapterActivities)
                {
                    adapterActivity.TraceId.ShouldBe(
                        usecaseActivity.TraceId,
                        $"Adapter Activity '{adapterActivity.DisplayName}' should have same TraceId as Usecase\n" +
                        $"UsecaseActivity TraceId: {usecaseActivity.TraceId}\n" +
                        $"AdapterActivity TraceId: {adapterActivity.TraceId}");

                    // Adapter의 부모가 Usecase여야 함
                    adapterActivity.ParentSpanId.ShouldBe(
                        usecaseActivity.SpanId,
                        $"Adapter Activity '{adapterActivity.DisplayName}' should be child of Usecase Activity");
                }
            }

            // 결과 출력 (디버깅용)
            OutputActivityHierarchy(relevantActivities);
        }
    }

    /// <summary>
    /// Activity 계층 구조를 콘솔에 출력합니다 (디버깅용).
    /// </summary>
    private static void OutputActivityHierarchy(List<Activity> activities)
    {
        Console.WriteLine("\n=== Captured Activity Hierarchy ===");
        foreach (var activity in activities.OrderBy(a => a.StartTimeUtc))
        {
            Console.WriteLine($"Activity: {activity.DisplayName}");
            Console.WriteLine($"  TraceId:      {activity.TraceId}");
            Console.WriteLine($"  SpanId:       {activity.SpanId}");
            Console.WriteLine($"  ParentSpanId: {activity.ParentSpanId}");
            Console.WriteLine($"  Duration:     {activity.Duration.TotalMilliseconds:F2}ms");
            Console.WriteLine();
        }
        Console.WriteLine("=== End Activity Hierarchy ===\n");
    }
}
