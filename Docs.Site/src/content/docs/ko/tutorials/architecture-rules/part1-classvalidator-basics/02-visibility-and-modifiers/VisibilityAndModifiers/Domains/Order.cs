namespace VisibilityAndModifiers.Domains;

public sealed class Order
{
    public string OrderNo { get; }

    private Order(string orderNo) => OrderNo = orderNo;

    public static Order Create(string orderNo) => new(orderNo);
}
