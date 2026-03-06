namespace CustomRules.Domains;

public sealed class Payment
{
    public string PaymentId { get; }
    public decimal Amount { get; }
    public string Method { get; }

    private Payment(string paymentId, decimal amount, string method)
    {
        PaymentId = paymentId;
        Amount = amount;
        Method = method;
    }

    public static Payment Create(string paymentId, decimal amount, string method)
        => new(paymentId, amount, method);
}
