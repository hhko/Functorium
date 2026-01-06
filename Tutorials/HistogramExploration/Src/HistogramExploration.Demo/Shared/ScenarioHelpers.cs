using System.Collections.Generic;

namespace HistogramExploration.Demo.Shared;

/// <summary>
/// 시나리오별 공통 헬퍼 메서드
/// </summary>
public static class ScenarioHelpers
{
    private static readonly Random Random = new();

    /// <summary>
    /// 랜덤 지연시간 생성 (밀리초)
    /// </summary>
    public static double GenerateRandomLatencyMs(double minMs, double maxMs)
    {
        return Random.NextDouble() * (maxMs - minMs) + minMs;
    }

    /// <summary>
    /// 정규 분포를 따르는 지연시간 생성 (근사치)
    /// </summary>
    public static double GenerateNormalLatencyMs(double meanMs, double stdDevMs)
    {
        // Box-Muller 변환을 사용한 정규 분포 근사
        double u1 = 1.0 - Random.NextDouble();
        double u2 = 1.0 - Random.NextDouble();
        double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return meanMs + z0 * stdDevMs;
    }

    /// <summary>
    /// SLO 임계값 근처에 집중된 지연시간 데이터 생성
    /// </summary>
    public static List<double> GenerateSloFocusedLatencies(int count, double sloThresholdMs, double spreadMs = 100)
    {
        var latencies = new List<double>();
        for (int i = 0; i < count; i++)
        {
            // SLO 임계값 근처에 집중 (예: 500ms ± 100ms)
            double latency = GenerateNormalLatencyMs(sloThresholdMs, spreadMs);
            latencies.Add(Math.Max(0, latency)); // 음수 방지
        }
        return latencies;
    }

    /// <summary>
    /// 다양한 범위의 지연시간 데이터 생성 (실제 시나리오 시뮬레이션)
    /// </summary>
    public static List<double> GenerateRealisticLatencies(int count)
    {
        var latencies = new List<double>();
        for (int i = 0; i < count; i++)
        {
            // 대부분 빠른 응답, 일부 느린 응답
            if (Random.NextDouble() < 0.8) // 80%는 빠른 응답
            {
                latencies.Add(GenerateNormalLatencyMs(100, 30)); // 평균 100ms
            }
            else if (Random.NextDouble() < 0.95) // 15%는 보통 응답
            {
                latencies.Add(GenerateNormalLatencyMs(300, 50)); // 평균 300ms
            }
            else // 5%는 느린 응답
            {
                latencies.Add(GenerateNormalLatencyMs(800, 200)); // 평균 800ms
            }
        }
        return latencies;
    }
}
