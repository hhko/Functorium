using System.Diagnostics.Metrics;
using HistogramExploration.Demo.Shared;

namespace HistogramExploration.Demo.Basic;

/// <summary>
/// Basic04: 백분위수(Percentile) 이해
/// 
/// 학습 목표:
/// - 백분위수가 무엇인지 이해
/// - P50, P90, P95, P99의 의미
/// - Histogram과 백분위수의 관계
/// - 실제 데이터로 백분위수 계산 및 해석
/// </summary>
public static class Basic04_Percentiles
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic04: Understanding Percentiles");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Console.WriteLine("📚 백분위수(Percentile)란?");
        Console.WriteLine();
        Console.WriteLine("백분위수는 데이터 집합에서 특정 비율의 값이 그보다 작거나 같은 값을 나타냅니다.");
        Console.WriteLine();
        Console.WriteLine("예시:");
        Console.WriteLine("  - P50 (중앙값): 50%의 값이 이 값보다 작거나 같음");
        Console.WriteLine("  - P95: 95%의 값이 이 값보다 작거나 같음");
        Console.WriteLine("  - P99: 99%의 값이 이 값보다 작거나 같음");
        Console.WriteLine();

        Console.WriteLine("💡 왜 백분위수가 중요한가?");
        Console.WriteLine();
        Console.WriteLine("평균값만으로는 전체 분포를 이해하기 어렵습니다:");
        Console.WriteLine("  - 평균: 100ms");
        Console.WriteLine("  - 하지만 일부 요청은 1000ms 이상 걸릴 수 있음");
        Console.WriteLine("  - P95를 보면 '대부분의 요청'이 얼마나 걸리는지 알 수 있음");
        Console.WriteLine();

        // 실제 데이터 생성 및 분석
        Console.WriteLine("📊 실제 데이터로 백분위수 계산:");
        Console.WriteLine();

        // 다양한 분포의 데이터 생성
        var latencies = new List<double>();
        Random random = new();

        // 대부분은 빠른 응답 (50-200ms)
        for (int i = 0; i < 80; i++)
        {
            latencies.Add(random.NextDouble() * 150 + 50);
        }

        // 일부는 보통 응답 (200-400ms)
        for (int i = 0; i < 15; i++)
        {
            latencies.Add(random.NextDouble() * 200 + 200);
        }

        // 소수는 느린 응답 (400-1000ms)
        for (int i = 0; i < 5; i++)
        {
            latencies.Add(random.NextDouble() * 600 + 400);
        }

        // 백분위수 계산 및 표시
        MetricViewer.PrintPercentiles(latencies, "Request Latencies");

        Console.WriteLine();
        Console.WriteLine("🔍 백분위수 해석:");
        Console.WriteLine();
        Console.WriteLine($"  P50 (중앙값): {CalculatePercentile(latencies, 50):F2}ms");
        Console.WriteLine("    → 절반의 요청이 이 시간 이내에 완료됨");
        Console.WriteLine();
        Console.WriteLine($"  P95: {CalculatePercentile(latencies, 95):F2}ms");
        Console.WriteLine("    → 95%의 요청이 이 시간 이내에 완료됨");
        Console.WriteLine("    → 5%의 요청만 이보다 느림 (tail latency)");
        Console.WriteLine();
        Console.WriteLine($"  P99: {CalculatePercentile(latencies, 99):F2}ms");
        Console.WriteLine("    → 99%의 요청이 이 시간 이내에 완료됨");
        Console.WriteLine("    → 1%의 요청만 이보다 느림 (extreme tail)");
        Console.WriteLine();

        Console.WriteLine("📈 Histogram과 백분위수의 관계:");
        Console.WriteLine();
        Console.WriteLine("Histogram은 값의 분포를 버킷으로 집계합니다.");
        Console.WriteLine("백분위수는 이 분포를 기반으로 계산됩니다:");
        Console.WriteLine();
        Console.WriteLine("  1. Histogram에 측정값 기록");
        Console.WriteLine("  2. 각 버킷에 몇 개의 값이 있는지 집계");
        Console.WriteLine("  3. 버킷 분포를 기반으로 백분위수 계산");
        Console.WriteLine();

        // Histogram 생성 및 기록
        Meter meter = new("HistogramExploration.Basic");
        Histogram<double> histogram = meter.CreateHistogram<double>(
            name: "basic.request.duration",
            unit: "s",
            description: "Request duration for percentile analysis");

        Console.WriteLine("Recording measurements to Histogram...");
        foreach (var latencyMs in latencies)
        {
            histogram.Record(latencyMs / 1000.0); // 밀리초를 초로 변환
        }

        Console.WriteLine($"✅ {latencies.Count} measurements recorded!");
        Console.WriteLine();
        Console.WriteLine("💡 실전 활용:");
        Console.WriteLine("  - SLO 설정: 'P95 ≤ 500ms' (95%의 요청이 500ms 이내)");
        Console.WriteLine("  - 성능 모니터링: P95/P99 추이를 관찰하여 성능 저하 감지");
        Console.WriteLine("  - 용량 계획: P99를 기준으로 인프라 용량 결정");
        Console.WriteLine("  - 사용자 경험: P95는 대부분의 사용자가 경험하는 성능");
        Console.WriteLine();
    }

    private static double CalculatePercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        var sorted = sortedValues.OrderBy(v => v).ToList();
        double index = (percentile / 100.0) * (sorted.Count - 1);
        int lowerIndex = (int)Math.Floor(index);
        int upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
            return sorted[lowerIndex];

        double weight = index - lowerIndex;
        return sorted[lowerIndex] * (1 - weight) + sorted[upperIndex] * weight;
    }
}
