using LoggerMessageLimits.Generated;

namespace LoggerMessageLimits.Usage;

public partial class OrderService
{
    [AutoLog]
    public void ProcessOrder(string orderId, string customerName, decimal amount)
    {
    }

    [AutoLog]
    public void AuditOrder(string orderId, string customerName, decimal amount,
        string date, string currency, string status, string warehouse)
    {
    }
}
