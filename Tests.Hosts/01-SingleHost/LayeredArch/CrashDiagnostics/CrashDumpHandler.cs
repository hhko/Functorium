using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LayeredArch.CrashDiagnostics;

/// <summary>
/// 크래시 덤프 생성을 위한 핸들러.
///
/// AccessViolationException과 같은 Corrupted State Exception(CSE)은
/// try-catch로 잡을 수 없지만, 다음 두 가지 방법으로 처리할 수 있습니다:
///
/// 1. AppDomain.CurrentDomain.UnhandledException 이벤트
///    - 예외 발생 후 프로세스 종료 전에 호출됩니다
///    - 덤프 파일 생성, 로깅 등을 수행할 수 있습니다
///    - 예외를 "처리"하여 프로세스 종료를 막을 수는 없습니다
///
/// 2. Windows Error Reporting (WER) 설정
///    - 시스템 수준에서 크래시 덤프를 자동으로 생성합니다
/// </summary>
public static class CrashDumpHandler
{
    private static string _dumpDirectory = string.Empty;
    private static bool _isInitialized;

    /// <summary>
    /// 크래시 덤프 핸들러를 초기화합니다.
    /// </summary>
    /// <param name="dumpDirectory">덤프 파일이 저장될 디렉터리 경로</param>
    public static void Initialize(string? dumpDirectory = null)
    {
        if (_isInitialized)
        {
            Console.WriteLine("[CrashDumpHandler] 이미 초기화되었습니다.");
            return;
        }

        _dumpDirectory = dumpDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LayeredArch",
            "CrashDumps");

        // 덤프 디렉터리 생성
        Directory.CreateDirectory(_dumpDirectory);

        // 처리되지 않은 예외 핸들러 등록
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // TaskScheduler의 처리되지 않은 예외 핸들러 등록
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _isInitialized = true;

        Console.WriteLine($"[CrashDumpHandler] 초기화 완료. 덤프 경로: {_dumpDirectory}");
    }

    /// <summary>
    /// 처리되지 않은 예외 발생 시 호출됩니다.
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        var isTerminating = e.IsTerminating;

        Console.WriteLine("================================================================");
        Console.WriteLine("[CrashDumpHandler] 처리되지 않은 예외 감지!");
        Console.WriteLine($"  예외 타입: {exception?.GetType().FullName ?? "Unknown"}");
        Console.WriteLine($"  메시지: {exception?.Message ?? "N/A"}");
        Console.WriteLine($"  프로세스 종료 예정: {isTerminating}");
        Console.WriteLine("================================================================");

        try
        {
            // 덤프 파일 생성
            var dumpPath = CreateMiniDump(exception);
            Console.WriteLine($"[CrashDumpHandler] 덤프 파일 생성 완료: {dumpPath}");
        }
        catch (Exception dumpEx)
        {
            Console.WriteLine($"[CrashDumpHandler] 덤프 생성 실패: {dumpEx.Message}");
        }

        // 예외 정보를 파일로 저장
        try
        {
            SaveExceptionInfo(exception);
        }
        catch
        {
            // 무시
        }
    }

    /// <summary>
    /// Task에서 관찰되지 않은 예외 발생 시 호출됩니다.
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Console.WriteLine($"[CrashDumpHandler] 관찰되지 않은 Task 예외: {e.Exception.Message}");
        // 관찰됨으로 표시하여 프로세스 종료 방지
        e.SetObserved();
    }

    /// <summary>
    /// 미니 덤프 파일을 생성합니다.
    /// </summary>
    private static string CreateMiniDump(Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var exceptionType = exception?.GetType().Name ?? "Unknown";
        var dumpFileName = $"crash_{exceptionType}_{timestamp}.dmp";
        var dumpPath = Path.Combine(_dumpDirectory, dumpFileName);

        // Windows에서만 MiniDumpWriteDump 사용 가능
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            CreateWindowsMiniDump(dumpPath);
        }
        else
        {
            // Linux/macOS에서는 createdump 또는 gcore 사용
            CreateUnixCoreDump(dumpPath);
        }

        return dumpPath;
    }

    /// <summary>
    /// Windows에서 MiniDumpWriteDump를 사용하여 덤프를 생성합니다.
    /// </summary>
    private static void CreateWindowsMiniDump(string dumpPath)
    {
        using var process = Process.GetCurrentProcess();
        using var fileStream = new FileStream(dumpPath, FileMode.Create, FileAccess.Write, FileShare.None);

        // MiniDumpWithFullMemory = 0x00000002
        // MiniDumpWithDataSegs = 0x00000001
        // MiniDumpWithHandleData = 0x00000004
        // MiniDumpWithThreadInfo = 0x00001000
        var dumpType = MiniDumpType.MiniDumpWithFullMemory;

        var result = MiniDumpWriteDump(
            process.Handle,
            (uint)process.Id,
            fileStream.SafeFileHandle.DangerousGetHandle(),
            dumpType,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (!result)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"MiniDumpWriteDump 실패. 오류 코드: {errorCode}");
        }
    }

    /// <summary>
    /// Unix 계열에서 코어 덤프를 생성합니다.
    /// </summary>
    private static void CreateUnixCoreDump(string dumpPath)
    {
        // .NET에서 제공하는 createdump 도구 사용
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
            ?? "/usr/share/dotnet";

        var createdumpPath = Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App",
            Environment.Version.ToString(), "createdump");

        if (!File.Exists(createdumpPath))
        {
            // 대안: 프로세스 정보만 텍스트로 저장
            var infoPath = dumpPath + ".info";
            using var process = Process.GetCurrentProcess();
            File.WriteAllText(infoPath, $"""
                Process Crash Information
                =========================
                PID: {process.Id}
                Name: {process.ProcessName}
                StartTime: {process.StartTime}
                WorkingSet: {process.WorkingSet64} bytes
                ThreadCount: {process.Threads.Count}
                Time: {DateTime.Now}

                Note: createdump tool not found at {createdumpPath}
                """);
            Console.WriteLine($"[CrashDumpHandler] createdump를 찾을 수 없어 정보 파일 생성: {infoPath}");
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        var startInfo = new ProcessStartInfo
        {
            FileName = createdumpPath,
            Arguments = $"-f {dumpPath} {currentProcess.Id}",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var dumpProcess = Process.Start(startInfo);
        dumpProcess?.WaitForExit(30000); // 30초 타임아웃
    }

    /// <summary>
    /// 예외 정보를 텍스트 파일로 저장합니다.
    /// </summary>
    private static void SaveExceptionInfo(Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var infoPath = Path.Combine(_dumpDirectory, $"crash_info_{timestamp}.txt");

        using var process = Process.GetCurrentProcess();
        var content = $"""
            ================================================================================
            Crash Report
            ================================================================================
            Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
            Process: {process.ProcessName} (PID: {process.Id})
            Machine: {Environment.MachineName}
            OS: {RuntimeInformation.OSDescription}
            .NET Version: {Environment.Version}
            Working Directory: {Environment.CurrentDirectory}

            ================================================================================
            Exception Details
            ================================================================================
            Type: {exception?.GetType().FullName ?? "Unknown"}
            Message: {exception?.Message ?? "N/A"}
            Source: {exception?.Source ?? "N/A"}
            TargetSite: {exception?.TargetSite?.ToString() ?? "N/A"}

            ================================================================================
            Stack Trace
            ================================================================================
            {exception?.StackTrace ?? "N/A"}

            ================================================================================
            Inner Exception
            ================================================================================
            {(exception?.InnerException != null ? $"Type: {exception.InnerException.GetType().FullName}\nMessage: {exception.InnerException.Message}\nStack: {exception.InnerException.StackTrace}" : "None")}
            ================================================================================
            """;

        File.WriteAllText(infoPath, content);
        Console.WriteLine($"[CrashDumpHandler] 예외 정보 저장 완료: {infoPath}");
    }

    #region Windows MiniDump P/Invoke

    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        uint processId,
        IntPtr hFile,
        MiniDumpType dumpType,
        IntPtr expParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);

    [Flags]
    private enum MiniDumpType : uint
    {
        MiniDumpNormal = 0x00000000,
        MiniDumpWithDataSegs = 0x00000001,
        MiniDumpWithFullMemory = 0x00000002,
        MiniDumpWithHandleData = 0x00000004,
        MiniDumpFilterMemory = 0x00000008,
        MiniDumpScanMemory = 0x00000010,
        MiniDumpWithUnloadedModules = 0x00000020,
        MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
        MiniDumpFilterModulePaths = 0x00000080,
        MiniDumpWithProcessThreadData = 0x00000100,
        MiniDumpWithPrivateReadWriteMemory = 0x00000200,
        MiniDumpWithoutOptionalData = 0x00000400,
        MiniDumpWithFullMemoryInfo = 0x00000800,
        MiniDumpWithThreadInfo = 0x00001000,
        MiniDumpWithCodeSegs = 0x00002000,
        MiniDumpWithoutAuxiliaryState = 0x00004000,
        MiniDumpWithFullAuxiliaryState = 0x00008000,
        MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
        MiniDumpIgnoreInaccessibleMemory = 0x00020000,
        MiniDumpWithTokenInformation = 0x00040000
    }

    #endregion
}
