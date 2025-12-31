using System.Diagnostics;
using Functorium.Adapters.Observabilities.Context;
using Functorium.Applications.Observabilities;
using Functorium.Applications.Observabilities.Context;
using Functorium.Applications.Observabilities.Spans;
using LanguageExt.Common;

namespace Functorium.Adapters.Observabilities.Spans;

/// <summary>
/// Activity를 래핑하는 ISpan 구현체입니다.
/// </summary>
internal sealed class OpenTelemetrySpan : ISpan
{
    private readonly Activity? _activity;
    private readonly ObservabilityContext _context;
    private bool _disposed;

    public OpenTelemetrySpan(Activity? activity)
    {
        _activity = activity;
        _context = activity != null
            ? ObservabilityContext.FromActivityContext(activity.Context)
            : ObservabilityContext.Create(string.Empty, string.Empty);
    }

    public string SpanId => _context.SpanId;

    public string TraceId => _context.TraceId;

    public IObservabilityContext Context => _context;

    /// <summary>
    /// 내부 Activity를 반환합니다. (내부 사용 전용)
    /// </summary>
    internal Activity? Activity => _activity;

    public void SetTag(string key, object? value)
    {
        _activity?.SetTag(key, value);
    }

    public void SetSuccess(double? elapsedMs = null)
    {
        if (elapsedMs.HasValue)
        {
            _activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsedMs.Value);
        }
        _activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
        _activity?.SetStatus(ActivityStatusCode.Ok);
    }

    public void SetFailure(string message, double? elapsedMs = null)
    {
        if (elapsedMs.HasValue)
        {
            _activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsedMs.Value);
        }
        _activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
        _activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorMessage, message);
        _activity?.SetStatus(ActivityStatusCode.Error, message);
    }

    public void SetFailure(Error error, double? elapsedMs = null)
    {
        SetFailure(error.Message, elapsedMs);
        _activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, error.GetType().Name);

        // ErrorCodeExpected나 ErrorCodeExceptional 등에서 Code 추출 시도
        if (error.Code != 0)
        {
            _activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, error.Code);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _activity?.Stop();
            _activity?.Dispose();
            _disposed = true;
        }
    }
}
