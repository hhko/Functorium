using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Intermediate;

/// <summary>
/// Intermediate03: 복잡한 태그 조합
/// 
/// 학습 목표:
/// - 태그 카디널리티 이해
/// - 메모리 최적화 팁
/// - TagList 사용법
/// </summary>
public static class Intermediate03_TagCombinations
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Intermediate03: Tag Combinations and Cardinality");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Intermediate");

        Histogram<double> requestDuration = meter.CreateHistogram<double>(
            name: "intermediate.request.duration",
            unit: "s",
            description: "Request duration with multiple tag dimensions");

        Console.WriteLine("Recording measurements with multiple tags...");
        Console.WriteLine();

        Random random = new();

        // 태그 값 정의
        string[] regions = { "us-east", "us-west", "eu-central" };
        string[] environments = { "production", "staging" };
        string[] httpMethods = { "GET", "POST", "PUT", "DELETE" };

        int measurementCount = 0;

        // 모든 태그 조합으로 측정값 기록
        foreach (var region in regions)
        {
            foreach (var environment in environments)
            {
                foreach (var httpMethod in httpMethods)
                {
                    double duration = random.NextDouble() * 0.5 + 0.1;

                    // TagList 사용 (Functorium 패턴 - 힙 할당 최소화)
                    TagList tags = new()
                    {
                        { "region", region },
                        { "environment", environment },
                        { "http.method", httpMethod }
                    };

                    requestDuration.Record(duration, tags);
                    measurementCount++;

                    Console.WriteLine($"  {region,-12} / {environment,-10} / {httpMethod,-6}: {duration * 1000:F2} ms");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"✅ Recorded {measurementCount} measurements");
        Console.WriteLine();

        // 태그 카디널리티 계산
        int totalCombinations = regions.Length * environments.Length * httpMethods.Length;
        Console.WriteLine("📊 Tag Cardinality Analysis:");
        Console.WriteLine($"   Regions: {regions.Length}");
        Console.WriteLine($"   Environments: {environments.Length}");
        Console.WriteLine($"   HTTP Methods: {httpMethods.Length}");
        Console.WriteLine($"   Total Combinations: {totalCombinations}");
        Console.WriteLine();

        Console.WriteLine("💡 Best Practices:");
        Console.WriteLine("   1. Keep tag cardinality low (< 1000 combinations per instrument)");
        Console.WriteLine("   2. Use TagList instead of KeyValuePair[] (allocation-free for ≤3 tags)");
        Console.WriteLine("   3. Avoid high-cardinality tags (e.g., user IDs, timestamps)");
        Console.WriteLine("   4. Prefer fixed sets of tag values over dynamic values");
        Console.WriteLine("   5. Use logs or databases for high-cardinality data");
        Console.WriteLine();

        Console.WriteLine("⚠️  Warning:");
        Console.WriteLine("   High cardinality (e.g., user IDs) can cause:");
        Console.WriteLine("   - Excessive memory usage");
        Console.WriteLine("   - Performance degradation");
        Console.WriteLine("   - Increased costs in metric collection tools");
        Console.WriteLine();
    }
}
