namespace LayeredArch.Application.Usecases.Orders.Commands;

public partial class CreateOrderCommandRequestLogEnricher
{
    partial void OnEnrichRequestLog(
        CreateOrderCommand.Request request,
        List<IDisposable> disposables)
    {
        // Computed: 전체 주문 수량 합계
        int totalQuantity = request.OrderLines.Sum(l => l.Quantity);
        // → ctx.create_order_command.request.order_total_quantity
        PushRequestCtx(disposables, "order_total_quantity", totalQuantity);
    }
}
