using System.Diagnostics;

namespace Functorium.Abstractions;

public static class ElapsedTimeCalculator
{
    public static double CalculateElapsedMilliseconds(long startTimestamp)
    {
        long endTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = endTimestamp - startTimestamp;
        return (double)elapsedTicks / Stopwatch.Frequency * 1000.0;
    }

    public static double CalculateElapsedSeconds(long startTimestamp)
    {
        long endTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = endTimestamp - startTimestamp;
        return (double)elapsedTicks / Stopwatch.Frequency;
    }

    public static long GetCurrentTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    public sealed class Timer : IDisposable
    {
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();

        public double ElapsedMilliseconds => CalculateElapsedMilliseconds(_startTimestamp);

        public double ElapsedSeconds => CalculateElapsedSeconds(_startTimestamp);

        public void Dispose()
        {
            // 특별한 정리 작업 없음
            // using 패턴을 위해 IDisposable 구현
        }
    }
}