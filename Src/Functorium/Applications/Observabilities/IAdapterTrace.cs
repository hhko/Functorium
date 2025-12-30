using System.Diagnostics;

namespace Functorium.Applications.Observabilities;

public interface IAdapterTrace
{
    Activity? Request(
        ActivityContext parentContext,
        string requestCategory,
        string requestHandler,
        string requestHandlerMethod,
        DateTimeOffset startTimestamp);

    void ResponseSuccess(Activity? activity, double elapsedMs);

    void ResponseFailure(Activity? activity, double elapsedMs, LanguageExt.Common.Error error);
}
