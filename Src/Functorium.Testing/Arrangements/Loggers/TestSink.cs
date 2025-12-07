using Serilog.Core;
using Serilog.Events;

namespace Functorium.Testing.Arrangements.Loggers;

public class TestSink : ILogEventSink
{
    private readonly List<LogEvent> _logEvents;

    public TestSink(List<LogEvent> logEvents)
    {
        _logEvents = logEvents;
    }

    public void Emit(LogEvent logEvent)
    {
        _logEvents.Add(logEvent);
    }
}

