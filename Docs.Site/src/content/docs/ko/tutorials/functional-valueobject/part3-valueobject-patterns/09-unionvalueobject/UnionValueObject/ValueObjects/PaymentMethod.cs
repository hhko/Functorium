namespace UnionValueObject.ValueObjects;

/// <summary>
/// 결제 수단을 표현하는 Discriminated Union 값 객체.
/// CreditCard | BankTransfer | Cash 중 정확히 하나.
/// </summary>
public abstract record PaymentMethod : UnionValueObject
{
    public sealed record CreditCard(string CardNumber, string ExpiryDate) : PaymentMethod;
    public sealed record BankTransfer(string AccountNumber, string BankCode) : PaymentMethod;
    public sealed record Cash() : PaymentMethod;

    public TResult Match<TResult>(
        Func<CreditCard, TResult> creditCard,
        Func<BankTransfer, TResult> bankTransfer,
        Func<Cash, TResult> cash) => this switch
    {
        CreditCard cc => creditCard(cc),
        BankTransfer bt => bankTransfer(bt),
        Cash c => cash(c),
        _ => throw new UnreachableCaseException(this)
    };

    /// <summary>
    /// 결제 수수료를 계산합니다.
    /// </summary>
    public decimal CalculateFee(decimal amount) => Match(
        creditCard: _ => amount * 0.03m,
        bankTransfer: _ => 1000m,
        cash: _ => 0m);

    /// <summary>
    /// 결제 수단의 표시 이름을 반환합니다.
    /// </summary>
    public string DisplayName => Match(
        creditCard: cc => $"신용카드 ({cc.CardNumber[^4..]})",
        bankTransfer: bt => $"계좌이체 ({bt.BankCode})",
        cash: _ => "현금");
}
