using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// Pipeline·Source generated 코드에서 처리 시간 계측에 사용하는 thin wrapper.
/// 모든 메서드가 hot path에서 요청당 2~3회 호출되므로 [AggressiveInlining]을 명시
/// 적용하여 JIT 자동 inline에 의존하지 않고 호출 오버헤드를 보장적으로 제거합니다.
/// 베이스 BCL Stopwatch.GetTimestamp() 자체가 [AggressiveInlining]을 적용한 thin
/// wrapper이므로 이를 wrapping한 본 메서드도 동일하게 적용해야 효과가 보존됩니다.
/// </summary>
public static class ElapsedTimeCalculator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateElapsedSeconds(long startTimestamp)
    {
        long endTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = endTimestamp - startTimestamp;
        return (double)elapsedTicks / Stopwatch.Frequency;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetCurrentTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    public sealed class Timer : IDisposable
    {
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();

        public double ElapsedSeconds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CalculateElapsedSeconds(_startTimestamp);
        }

        public void Dispose()
        {
            // 특별한 정리 작업 없음
            // using 패턴을 위해 IDisposable 구현
        }
    }
}
