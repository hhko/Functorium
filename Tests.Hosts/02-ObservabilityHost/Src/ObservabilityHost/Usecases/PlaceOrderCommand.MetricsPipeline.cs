using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Usecases;

using Mediator;

using Microsoft.Extensions.Options;

namespace ObservabilityHost.Usecases;

public sealed class PlaceOrderMetricsPipeline
    : UsecaseMetricCustomPipelineBase<PlaceOrderCommand.Request>
    , IPipelineBehavior<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>
{
    private readonly Histogram<int> _orderLineCount;
    private readonly Histogram<double> _orderTotalAmount;

    public PlaceOrderMetricsPipeline(
        IOptions<OpenTelemetryOptions> options, IMeterFactory meterFactory)
        : base(options.Value.ServiceNamespace, meterFactory)
    {
        _orderLineCount = _meter.CreateHistogram<int>(
            name: GetMetricName("order_line_count"),
            unit: "{lines}",
            description: "Number of order lines per PlaceOrder");

        _orderTotalAmount = _meter.CreateHistogram<double>(
            name: GetMetricName("order_total_amount"),
            unit: "{currency}",
            description: "Total amount per PlaceOrder");
    }

    public async ValueTask<FinResponse<PlaceOrderCommand.Response>> Handle(
        PlaceOrderCommand.Request request,
        MessageHandlerDelegate<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>> next,
        CancellationToken ct)
    {
        _orderLineCount.Record(request.Lines.Count);
        _orderTotalAmount.Record((double)request.Lines.Sum(l => l.Quantity * l.UnitPrice));
        return await next(request, ct);
    }
}
