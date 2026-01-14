using Functorium.Testing.Arrangements.Loggers;
using Functorium.Testing.Assertions.Logging;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Functorium.Testing.Arrangements.Logging;

/// <summary>
/// 로그 테스트를 위한 컨텍스트 클래스.
/// 구조화된 로그 캡처와 검증을 위한 인프라를 제공합니다.
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// var context = new LogTestContext();
/// var logger = context.CreateLogger&lt;MyClass&gt;();
///
/// // ... 로그를 생성하는 코드 실행 ...
///
/// var logData = context.ExtractRequestLogData();
/// await Verify(logData);
/// </code>
/// </remarks>
public sealed class LogTestContext : IDisposable
{
    private readonly List<LogEvent> _logEvents = [];
    private readonly Serilog.ILogger _serilogLogger;
    private bool _disposed;

    /// <summary>
    /// 기본 최소 로그 레벨(Debug)로 LogTestContext를 초기화합니다.
    /// </summary>
    public LogTestContext() : this(LogEventLevel.Debug)
    {
    }

    /// <summary>
    /// 지정된 최소 로그 레벨로 LogTestContext를 초기화합니다.
    /// </summary>
    /// <param name="minimumLevel">캡처할 최소 로그 레벨</param>
    public LogTestContext(LogEventLevel minimumLevel)
    {
        var sink = new TestSink(_logEvents);
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.Sink(sink)
            .CreateLogger();
    }

    /// <summary>
    /// 캡처된 모든 로그 이벤트 목록
    /// </summary>
    public IReadOnlyList<LogEvent> LogEvents => _logEvents;

    /// <summary>
    /// 캡처된 로그 이벤트 수
    /// </summary>
    public int LogCount => _logEvents.Count;

    /// <summary>
    /// 지정된 타입에 대한 구조화된 테스트 로거를 생성합니다.
    /// </summary>
    /// <typeparam name="T">로거의 카테고리 타입</typeparam>
    /// <returns>ILogger&lt;T&gt; 인스턴스</returns>
    public ILogger<T> CreateLogger<T>()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new StructuredTestLogger<T>(_serilogLogger);
    }

    /// <summary>
    /// 첫 번째 로그 이벤트(일반적으로 Request 로그)를 반환합니다.
    /// </summary>
    /// <returns>첫 번째 LogEvent</returns>
    /// <exception cref="InvalidOperationException">로그 이벤트가 없는 경우</exception>
    public LogEvent GetFirstLog()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_logEvents.Count == 0)
            throw new InvalidOperationException("캡처된 로그 이벤트가 없습니다.");

        return _logEvents[0];
    }

    /// <summary>
    /// 두 번째 로그 이벤트(일반적으로 Response 로그)를 반환합니다.
    /// </summary>
    /// <returns>두 번째 LogEvent</returns>
    /// <exception cref="InvalidOperationException">로그 이벤트가 2개 미만인 경우</exception>
    public LogEvent GetSecondLog()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_logEvents.Count < 2)
            throw new InvalidOperationException($"캡처된 로그 이벤트가 2개 미만입니다. 현재: {_logEvents.Count}개");

        return _logEvents[1];
    }

    /// <summary>
    /// 지정된 인덱스의 로그 이벤트를 반환합니다.
    /// </summary>
    /// <param name="index">로그 이벤트 인덱스 (0-based)</param>
    /// <returns>해당 인덱스의 LogEvent</returns>
    /// <exception cref="ArgumentOutOfRangeException">인덱스가 범위를 벗어난 경우</exception>
    public LogEvent GetLogAt(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (index < 0 || index >= _logEvents.Count)
            throw new ArgumentOutOfRangeException(nameof(index),
                $"인덱스가 범위를 벗어났습니다. 인덱스: {index}, 로그 수: {_logEvents.Count}");

        return _logEvents[index];
    }

    /// <summary>
    /// 지정된 로그 레벨의 모든 로그 이벤트를 반환합니다.
    /// </summary>
    /// <param name="level">필터링할 로그 레벨</param>
    /// <returns>해당 레벨의 LogEvent 목록</returns>
    public IReadOnlyList<LogEvent> GetLogsByLevel(LogEventLevel level)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _logEvents.Where(e => e.Level == level).ToList();
    }

    /// <summary>
    /// 첫 번째 로그 이벤트의 데이터를 익명 객체로 추출합니다.
    /// </summary>
    /// <returns>Verify 스냅샷 테스트용 익명 객체</returns>
    public object ExtractFirstLogData()
    {
        return LogEventPropertyExtractor.ExtractLogData(GetFirstLog());
    }

    /// <summary>
    /// 두 번째 로그 이벤트의 데이터를 익명 객체로 추출합니다.
    /// </summary>
    /// <returns>Verify 스냅샷 테스트용 익명 객체</returns>
    public object ExtractSecondLogData()
    {
        return LogEventPropertyExtractor.ExtractLogData(GetSecondLog());
    }

    /// <summary>
    /// 지정된 인덱스의 로그 이벤트 데이터를 익명 객체로 추출합니다.
    /// </summary>
    /// <param name="index">로그 이벤트 인덱스 (0-based)</param>
    /// <returns>Verify 스냅샷 테스트용 익명 객체</returns>
    public object ExtractLogDataAt(int index)
    {
        return LogEventPropertyExtractor.ExtractLogData(GetLogAt(index));
    }

    /// <summary>
    /// 모든 로그 이벤트의 데이터를 익명 객체 목록으로 추출합니다.
    /// </summary>
    /// <returns>Verify 스냅샷 테스트용 익명 객체 목록</returns>
    public IEnumerable<object> ExtractAllLogData()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return LogEventPropertyExtractor.ExtractLogData(_logEvents);
    }

    /// <summary>
    /// 캡처된 모든 로그 이벤트를 지웁니다.
    /// </summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _logEvents.Clear();
    }

    /// <summary>
    /// 리소스를 해제합니다.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        (_serilogLogger as IDisposable)?.Dispose();
        _disposed = true;
    }
}
