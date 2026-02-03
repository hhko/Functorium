using System.Runtime.InteropServices;

namespace LayeredArch.CrashDiagnostics;

/// <summary>
/// AccessViolationException 발생 및 처리 데모.
///
/// AccessViolationException은 CLR 수준의 Corrupted State Exception(CSE)으로,
/// .NET Core/.NET 5+ 에서는 기본적으로 try-catch로 잡을 수 없습니다.
/// </summary>
public static class AccessViolationDemo
{
    /// <summary>
    /// AccessViolationException을 의도적으로 발생시킵니다.
    /// Marshal.WriteInt32를 사용하여 잘못된 메모리 주소(0x0)에 쓰기를 시도합니다.
    /// </summary>
    public static void TriggerAccessViolation()
    {
        // 잘못된 메모리 주소(null pointer)에 쓰기 시도
        // 이는 AccessViolationException을 발생시킵니다
        Marshal.WriteInt32(IntPtr.Zero, 0);
    }

    /// <summary>
    /// try-catch로 AccessViolationException을 잡으려는 시도 (실패함).
    ///
    /// .NET Framework에서는 [HandleProcessCorruptedStateExceptions] 특성으로
    /// CSE를 잡을 수 있었지만, .NET Core/.NET 5+ 에서는 이 방식이 작동하지 않습니다.
    /// </summary>
    public static string DemonstrateTryCatchFailure()
    {
        try
        {
            Console.WriteLine("[Demo] try 블록 진입...");
            Console.WriteLine("[Demo] AccessViolationException 발생 시도...");

            TriggerAccessViolation();

            // 이 코드는 실행되지 않습니다
            return "예외가 발생하지 않음 (예상하지 못한 결과)";
        }
        catch (AccessViolationException ex)
        {
            // .NET Core/.NET 5+ 에서는 이 catch 블록에 도달하지 않습니다
            // 프로세스가 즉시 종료됩니다
            return $"AccessViolationException 캐치됨: {ex.Message}";
        }
        catch (Exception ex)
        {
            // 일반 Exception으로도 잡을 수 없습니다
            return $"일반 Exception 캐치됨: {ex.GetType().Name} - {ex.Message}";
        }
        finally
        {
            // finally 블록도 실행되지 않을 수 있습니다
            Console.WriteLine("[Demo] finally 블록 (실행되지 않을 수 있음)");
        }
    }
}
