using System.Diagnostics;

namespace Functorium.Applications.Observabilities;

public interface IAdapterMetric
{
    void Request(
        Activity? activity,
        string requestCategory,
        string requestHandler,
        string requestHandlerMethod,
        DateTimeOffset startTimestamp);

    void ResponseSuccess(Activity? activity, string requestCategory, double elapsedMs);

    void ResponseFailure(Activity? activity, string requestCategory, double elapsedMs, LanguageExt.Common.Error error);
}
