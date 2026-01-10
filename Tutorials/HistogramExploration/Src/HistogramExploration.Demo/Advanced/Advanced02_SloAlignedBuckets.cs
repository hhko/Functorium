using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities.Configurations;

namespace HistogramExploration.Demo.Advanced;

/// <summary>
/// Advanced02: SLO 정렬 버킷
/// 
/// 학습 목표:
/// - SloConfiguration.DefaultHistogramBuckets 사용
/// - SLO 목표값과 버킷 정렬의 중요성
/// - 백분위수 계산 정확도 향상
/// </summary>
public static class Advanced02_SloAlignedBuckets
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced02: SLO-Aligned Buckets");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Advanced");

        // Functorium의 DefaultHistogramBuckets 사용
        double[] sloAlignedBuckets = SloConfiguration.DefaultHistogramBuckets;

        Console.WriteLine("Functorium DefaultHistogramBuckets:");
        Console.WriteLine($"   [{string.Join(", ", sloAlignedBuckets.Select(b => $"{b * 1000:F0}ms"))}]");
        Console.WriteLine();

        Console.WriteLine("SLO Alignment Analysis:");
        Console.WriteLine($"   Command SLO P95: 500ms → Bucket boundary: {(sloAlignedBuckets.Contains(0.5) ? "✅ ALIGNED" : "❌ NOT ALIGNED")}");
        Console.WriteLine($"   Command SLO P99: 1000ms → Bucket boundary: {(sloAlignedBuckets.Contains(1.0) ? "✅ ALIGNED" : "❌ NOT ALIGNED")}");
        Console.WriteLine($"   Query SLO P95: 200ms → Bucket boundary: {(sloAlignedBuckets.Contains(0.2) ? "✅ ALIGNED" : "❌ NOT ALIGNED")}");
        Console.WriteLine($"   Query SLO P99: 500ms → Bucket boundary: {(sloAlignedBuckets.Contains(0.5) ? "✅ ALIGNED" : "❌ NOT ALIGNED")}");
        Console.WriteLine();

        Histogram<double> histogram = meter.CreateHistogram<double>(
            name: "advanced.slo_aligned.duration",
            unit: "s",
            description: "Duration with SLO-aligned buckets",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = sloAlignedBuckets
            });

        Console.WriteLine("Recording measurements...");
        Console.WriteLine();

        Random random = new();
        for (int i = 0; i < 100; i++)
        {
            // SLO 임계값 근처에 집중된 데이터 생성
            double duration;
            if (i < 50)
            {
                // 50%는 빠른 응답 (100-300ms)
                duration = random.NextDouble() * 0.2 + 0.1;
            }
            else if (i < 95)
            {
                // 45%는 보통 응답 (300-500ms)
                duration = random.NextDouble() * 0.2 + 0.3;
            }
            else
            {
                // 5%는 느린 응답 (500ms+)
                duration = random.NextDouble() * 1.0 + 0.5;
            }

            histogram.Record(duration);
        }

        Console.WriteLine("✅ Measurements recorded with SLO-aligned buckets!");
        Console.WriteLine();
        Console.WriteLine("💡 Why SLO Alignment Matters:");
        Console.WriteLine("   - When SLO threshold (e.g., 500ms) is a bucket boundary,");
        Console.WriteLine("     you can accurately determine if measurements exceed the threshold");
        Console.WriteLine("   - Without alignment, P95/P99 calculations become less accurate");
        Console.WriteLine("   - Functorium's buckets are designed to align with common SLO thresholds");
        Console.WriteLine();
    }
}
