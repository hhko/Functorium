namespace CustomRules.Domains;

public sealed class Invoice
{
    public string InvoiceNo { get; }
    public decimal Amount { get; }

    private Invoice(string invoiceNo, decimal amount)
    {
        InvoiceNo = invoiceNo;
        Amount = amount;
    }

    public static Invoice Create(string invoiceNo, decimal amount)
        => new(invoiceNo, amount);
}
