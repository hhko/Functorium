namespace LayeredArch.CrashDiagnostics;

/// <summary>
/// 크래시 진단용 API 엔드포인트.
///
/// ⚠️ 경고: 이 엔드포인트들은 진단 목적으로만 사용해야 합니다.
/// 프로덕션 환경에서는 반드시 비활성화하세요.
/// </summary>
public static class CrashDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapCrashDiagnosticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnostics/crash")
            .WithTags("Crash Diagnostics")
            .WithDescription("크래시 진단용 엔드포인트 (개발 전용)");

        // 1. AccessViolationException 발생 (프로세스 크래시)
        group.MapPost("/access-violation", TriggerAccessViolation)
            .WithName("TriggerAccessViolation")
            .WithSummary("AccessViolationException을 발생시킵니다 (프로세스가 종료됩니다)")
            .Produces<string>(StatusCodes.Status200OK);

        // 2. try-catch 테스트 (catch되지 않음을 증명)
        group.MapPost("/try-catch-demo", DemonstrateTryCatchFailure)
            .WithName("DemonstrateTryCatchFailure")
            .WithSummary("try-catch로 AccessViolationException을 잡으려는 시도 (실패함)")
            .Produces<string>(StatusCodes.Status200OK);

        // 3. 일반 예외 테스트 (비교용)
        group.MapPost("/normal-exception", TriggerNormalException)
            .WithName("TriggerNormalException")
            .WithSummary("일반 예외를 발생시킵니다 (catch 가능)")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status500InternalServerError);

        // 4. 덤프 디렉터리 확인
        group.MapGet("/dump-directory", GetDumpDirectory)
            .WithName("GetDumpDirectory")
            .WithSummary("크래시 덤프 저장 디렉터리를 반환합니다")
            .Produces<DumpDirectoryResponse>(StatusCodes.Status200OK);

        // 5. 덤프 파일 목록
        group.MapGet("/dumps", ListDumpFiles)
            .WithName("ListDumpFiles")
            .WithSummary("생성된 덤프 파일 목록을 반환합니다")
            .Produces<DumpFilesResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    /// <summary>
    /// AccessViolationException을 발생시킵니다.
    /// 이 요청 후 프로세스가 종료됩니다.
    /// </summary>
    private static IResult TriggerAccessViolation()
    {
        Console.WriteLine("================================================================");
        Console.WriteLine("[API] AccessViolationException 발생 요청 수신");
        Console.WriteLine("[API] 경고: 프로세스가 즉시 종료됩니다!");
        Console.WriteLine("================================================================");

        // 응답을 먼저 보내기 위해 약간의 지연 후 크래시 발생
        _ = Task.Run(async () =>
        {
            await Task.Delay(100); // 응답 전송 대기
            AccessViolationDemo.TriggerAccessViolation();
        });

        return Results.Ok("""
            ⚠️ AccessViolationException이 100ms 후 발생합니다.
            프로세스가 종료되고, CrashDumpHandler가 덤프 파일을 생성합니다.

            덤프 파일 위치를 확인하려면:
            GET /api/diagnostics/crash/dump-directory

            생성된 덤프 파일 목록:
            GET /api/diagnostics/crash/dumps
            """);
    }

    /// <summary>
    /// try-catch로 AccessViolationException을 잡으려는 시도를 보여줍니다.
    /// </summary>
    private static IResult DemonstrateTryCatchFailure()
    {
        Console.WriteLine("================================================================");
        Console.WriteLine("[API] try-catch 데모 시작");
        Console.WriteLine("[API] AccessViolationException은 try-catch로 잡을 수 없습니다");
        Console.WriteLine("[API] 프로세스가 종료됩니다!");
        Console.WriteLine("================================================================");

        // 이 메서드 내부에서 try-catch를 시도하지만, catch되지 않고 프로세스가 종료됩니다
        var result = AccessViolationDemo.DemonstrateTryCatchFailure();

        // 이 코드는 실행되지 않습니다
        return Results.Ok(result);
    }

    /// <summary>
    /// 일반 예외를 발생시킵니다 (비교용).
    /// </summary>
    private static IResult TriggerNormalException(bool shouldCatch = true)
    {
        Console.WriteLine("================================================================");
        Console.WriteLine("[API] 일반 예외 발생 테스트");
        Console.WriteLine($"[API] catch 여부: {shouldCatch}");
        Console.WriteLine("================================================================");

        if (shouldCatch)
        {
            try
            {
                throw new InvalidOperationException("이것은 일반 예외입니다 - catch 가능");
            }
            catch (Exception ex)
            {
                return Results.Ok($"""
                    ✅ 일반 예외가 성공적으로 catch되었습니다.

                    예외 타입: {ex.GetType().Name}
                    메시지: {ex.Message}

                    일반 예외는 try-catch로 잡을 수 있지만,
                    AccessViolationException은 CLR 수준의 CSE(Corrupted State Exception)로
                    .NET Core/.NET 5+ 에서는 catch할 수 없습니다.
                    """);
            }
        }
        else
        {
            // catch 없이 예외 발생 - UnhandledException 핸들러가 처리
            throw new InvalidOperationException("이것은 처리되지 않은 일반 예외입니다");
        }
    }

    /// <summary>
    /// 덤프 디렉터리 경로를 반환합니다.
    /// </summary>
    private static IResult GetDumpDirectory()
    {
        var dumpDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LayeredArch",
            "CrashDumps");

        return Results.Ok(new DumpDirectoryResponse(
            dumpDir,
            Directory.Exists(dumpDir)));
    }

    /// <summary>
    /// 생성된 덤프 파일 목록을 반환합니다.
    /// </summary>
    private static IResult ListDumpFiles()
    {
        var dumpDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LayeredArch",
            "CrashDumps");

        if (!Directory.Exists(dumpDir))
        {
            return Results.Ok(new DumpFilesResponse(dumpDir, []));
        }

        var files = Directory.GetFiles(dumpDir)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new DumpFileInfo(
                f.Name,
                f.FullName,
                f.Length,
                f.CreationTime))
            .ToArray();

        return Results.Ok(new DumpFilesResponse(dumpDir, files));
    }
}

public record DumpDirectoryResponse(string Path, bool Exists);
public record DumpFilesResponse(string Directory, DumpFileInfo[] Files);
public record DumpFileInfo(string Name, string FullPath, long SizeBytes, DateTime CreatedAt);
