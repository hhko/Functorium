using System.Diagnostics;

// Source Generator가 생성하는 코드에서 사용하는 네임스페이스
namespace Functorium.Adapters.Observabilities.Abstractions.Utilities;

/// <summary>
/// 경과 시간 계산 유틸리티 (Mock).
/// Stopwatch를 사용하여 고정밀 시간 측정을 제공합니다.
/// </summary>
public static class ElapsedTimeCalculator
{
    public static long GetCurrentTimestamp() => Stopwatch.GetTimestamp();

    public static double CalculateElapsedMilliseconds(long startTimestamp)
    {
        long endTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = endTimestamp - startTimestamp;
        return (double)elapsedTicks / Stopwatch.Frequency * 1000.0;
    }
}
