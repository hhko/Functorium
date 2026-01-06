using System.Diagnostics;
using System.Diagnostics.Metrics;
using HistogramExploration.Demo.Shared;

namespace HistogramExploration.Demo.Advanced;

/// <summary>
/// Advanced03: HTTP 요청 지연시간 측정 시나리오
/// 
/// 학습 목표:
/// - 실제 웹 API 패턴 시뮬레이션
/// - P95, P99 백분위수 분석
/// - 태그를 사용한 요청 분류
/// </summary>
public static class Advanced03_RequestLatencyScenario
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced03: HTTP Request Latency Scenario");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Advanced");

        Histogram<double> requestDuration = meter.CreateHistogram<double>(
            name: "advanced.http.request.duration",
            unit: "s",
            description: "HTTP request processing duration",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0]
            });

        Console.WriteLine("Simulating HTTP requests...");
        Console.WriteLine();

        string[] endpoints = { "/api/users", "/api/products", "/api/orders", "/api/payments" };
        string[] httpMethods = { "GET", "POST", "PUT", "DELETE" };
        int[] statusCodes = { 200, 201, 400, 404, 500 };

        var allLatencies = new List<double>();

        Random random = new();

        foreach (var endpoint in endpoints)
        {
            foreach (var method in httpMethods)
            {
                // 엔드포인트와 메서드에 따라 다른 처리 시간 시뮬레이션
                double baseTime = endpoint switch
                {
                    "/api/payments" => 0.3,  // 결제는 느림
                    "/api/orders" => 0.2,     // 주문은 보통
                    "/api/products" => 0.1,   // 제품 조회는 빠름
                    "/api/users" => 0.15,     // 사용자 조회는 빠름
                    _ => 0.2
                };

                int statusCode = statusCodes[random.Next(statusCodes.Length)];

                // 상태 코드에 따라 약간의 변동 추가
                double duration = baseTime + random.NextDouble() * 0.2;
                if (statusCode >= 500)
                {
                    duration += 0.5; // 에러 응답은 더 느림
                }

                TagList tags = new()
                {
                    { "http.method", method },
                    { "http.route", endpoint },
                    { "http.status_code", statusCode.ToString() }
                };

                requestDuration.Record(duration, tags);
                allLatencies.Add(duration * 1000); // 밀리초로 변환

                Console.WriteLine($"  {method,-6} {endpoint,-20} [{statusCode}] {duration * 1000:F2} ms");
            }
        }

        Console.WriteLine();
        Console.WriteLine("📊 Latency Analysis:");
        MetricViewer.PrintPercentiles(allLatencies, "HTTP Request Latencies");

        Console.WriteLine();
        Console.WriteLine("💡 Real-world Usage:");
        Console.WriteLine("   - Monitor API performance");
        Console.WriteLine("   - Identify slow endpoints");
        Console.WriteLine("   - Set up alerts for P95/P99 thresholds");
        Console.WriteLine("   - Analyze performance by endpoint, method, or status code");
        Console.WriteLine();
    }
}
