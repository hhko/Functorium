using System.Diagnostics;
using System.Diagnostics.Metrics;
using HistogramExploration.Demo.Shared;

namespace HistogramExploration.Demo.Advanced;

/// <summary>
/// Advanced04: 데이터베이스 쿼리 실행시간 측정
/// 
/// 학습 목표:
/// - 쿼리 타입별 분류 (SELECT, INSERT, UPDATE)
/// - 느린 쿼리 감지
/// - 데이터베이스 성능 모니터링 패턴
/// </summary>
public static class Advanced04_DatabaseQueryScenario
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced04: Database Query Duration Scenario");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Advanced");

        Histogram<double> queryDuration = meter.CreateHistogram<double>(
            name: "advanced.database.query.duration",
            unit: "s",
            description: "Database query execution duration",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = [0.001, 0.005, 0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5]
            });

        Console.WriteLine("Simulating database queries...");
        Console.WriteLine();

        string[] queryTypes = { "SELECT", "INSERT", "UPDATE", "DELETE" };
        string[] tables = { "users", "products", "orders", "payments" };

        var allLatencies = new List<double>();

        Random random = new();

        foreach (var queryType in queryTypes)
        {
            foreach (var table in tables)
            {
                // 쿼리 타입과 테이블에 따라 다른 실행 시간 시뮬레이션
                double baseTime = queryType switch
                {
                    "SELECT" => 0.01,   // 읽기는 빠름
                    "INSERT" => 0.05,   // 쓰기는 보통
                    "UPDATE" => 0.08,   // 업데이트는 느림
                    "DELETE" => 0.1,    // 삭제는 가장 느림
                    _ => 0.05
                };

                // 테이블 크기에 따른 변동 추가
                double tableMultiplier = table switch
                {
                    "users" => 1.0,
                    "products" => 1.2,
                    "orders" => 1.5,
                    "payments" => 2.0, // 결제 테이블은 더 느림
                    _ => 1.0
                };

                double duration = baseTime * tableMultiplier + random.NextDouble() * 0.05;

                TagList tags = new()
                {
                    { "db.operation", queryType },
                    { "db.table", table }
                };

                queryDuration.Record(duration, tags);
                allLatencies.Add(duration * 1000); // 밀리초로 변환

                Console.WriteLine($"  {queryType,-6} {table,-15} {duration * 1000:F2} ms");
            }
        }

        Console.WriteLine();
        Console.WriteLine("📊 Query Duration Analysis:");
        MetricViewer.PrintPercentiles(allLatencies, "Database Query Durations");

        // 느린 쿼리 감지 시뮬레이션
        double slowQueryThreshold = 100; // 100ms
        var slowQueries = allLatencies.Where(l => l > slowQueryThreshold).ToList();

        Console.WriteLine();
        Console.WriteLine($"⚠️  Slow Queries (> {slowQueryThreshold}ms): {slowQueries.Count} queries");
        if (slowQueries.Any())
        {
            Console.WriteLine($"   Average: {slowQueries.Average():F2} ms");
            Console.WriteLine($"   Max: {slowQueries.Max():F2} ms");
        }

        Console.WriteLine();
        Console.WriteLine("💡 Real-world Usage:");
        Console.WriteLine("   - Monitor database performance");
        Console.WriteLine("   - Identify slow queries");
        Console.WriteLine("   - Optimize queries based on performance data");
        Console.WriteLine("   - Set up alerts for query duration thresholds");
        Console.WriteLine();
    }
}
