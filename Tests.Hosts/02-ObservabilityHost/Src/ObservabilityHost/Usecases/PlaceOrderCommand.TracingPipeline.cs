using System.Diagnostics;

using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Usecases;

using Mediator;

namespace ObservabilityHost.Usecases;

public sealed class PlaceOrderTracingPipeline
    : UsecaseTracingCustomPipelineBase<PlaceOrderCommand.Request>
    , IPipelineBehavior<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>
{
    public PlaceOrderTracingPipeline(ActivitySource activitySource) : base(activitySource) { }

    public async ValueTask<FinResponse<PlaceOrderCommand.Response>> Handle(
        PlaceOrderCommand.Request request,
        MessageHandlerDelegate<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>> next,
        CancellationToken ct)
    {
        using Activity? activity = StartCustomActivity("ValidateOrder");
        if (activity != null)
        {
            SetStandardRequestTags(activity, "ValidateOrder");
            activity.SetTag("order.line_count", request.Lines.Count);
            activity.SetTag("order.customer_id", request.CustomerId);
        }

        return await next(request, ct);
    }
}
