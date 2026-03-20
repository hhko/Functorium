using Functorium.Applications.Usecases;

namespace ObservabilityHost.Usecases;

/// <summary>
/// PlaceOrderCommand의 커스텀 LogEnricher 확장.
/// 자동 생성된 코드에 computed 속성(order_total_amount, average_line_amount)을 추가합니다.
/// </summary>
public partial class PlaceOrderCommandRequestLogEnricher
{
    partial void OnEnrichRequestLog(
        PlaceOrderCommand.Request request,
        List<IDisposable> disposables)
    {
        decimal total = request.Lines.Sum(l => l.Quantity * l.UnitPrice);
        // → ctx.place_order_command.request.order_total_amount
        PushRequestCtx(disposables, "order_total_amount", total);
    }

    partial void OnEnrichResponseLog(
        PlaceOrderCommand.Request request,
        FinResponse<PlaceOrderCommand.Response> response,
        List<IDisposable> disposables)
    {
        if (response is FinResponse<PlaceOrderCommand.Response>.Succ { Value: var r } && r.LineCount > 0)
        {
            // → ctx.place_order_command.response.average_line_amount
            PushResponseCtx(disposables, "average_line_amount", r.TotalAmount / r.LineCount);
        }
    }
}
