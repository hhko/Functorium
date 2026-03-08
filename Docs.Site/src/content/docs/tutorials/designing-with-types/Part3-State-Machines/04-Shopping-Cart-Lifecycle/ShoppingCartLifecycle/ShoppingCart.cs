using LanguageExt;
using Functorium.Domains.Errors;

namespace ShoppingCartLifecycle;

/// <summary>
/// 쇼핑 카트 상태 기계: Empty → Active → Paid
/// 상태 건너뛰기 불가 — 타입으로 보장
/// </summary>
public abstract record ShoppingCart
{
    public sealed record Empty() : ShoppingCart;
    public sealed record Active(List<string> Items) : ShoppingCart;
    public sealed record Paid(List<string> Items, decimal Amount, DateTime PaidAt) : ShoppingCart;

    private ShoppingCart() { }

    /// <summary>
    /// 아이템 추가: Empty → Active, Active → Active
    /// Paid 상태에서는 추가 불가
    /// </summary>
    public static Fin<ShoppingCart> AddItem(ShoppingCart cart, string item) => cart switch
    {
        Empty => new Active([item]),
        Active a => new Active([.. a.Items, item]),
        Paid => Fin.Fail<ShoppingCart>(
            DomainError.For<ShoppingCart>(
                new DomainErrorType.InvalidTransition(FromState: "Paid", ToState: "Active"),
                cart.ToString()!,
                "결제 완료된 카트에 아이템을 추가할 수 없습니다")),
        _ => throw new InvalidOperationException()
    };

    /// <summary>
    /// 결제: Active → Paid
    /// Empty, Paid 상태에서는 결제 불가
    /// </summary>
    public static Fin<ShoppingCart> Pay(ShoppingCart cart, decimal amount) => cart switch
    {
        Active a => new Paid(a.Items, amount, DateTime.UtcNow),
        Empty => Fin.Fail<ShoppingCart>(
            DomainError.For<ShoppingCart>(
                new DomainErrorType.InvalidTransition(FromState: "Empty", ToState: "Paid"),
                cart.ToString()!,
                "빈 카트는 결제할 수 없습니다")),
        Paid => Fin.Fail<ShoppingCart>(
            DomainError.For<ShoppingCart>(
                new DomainErrorType.InvalidTransition(FromState: "Paid", ToState: "Paid"),
                cart.ToString()!,
                "이미 결제된 카트입니다")),
        _ => throw new InvalidOperationException()
    };

    /// <summary>
    /// 아이템 모두 제거: Active → Empty
    /// </summary>
    public static Fin<ShoppingCart> RemoveAllItems(ShoppingCart cart) => cart switch
    {
        Active => new Empty(),
        Empty => Fin.Fail<ShoppingCart>(
            DomainError.For<ShoppingCart>(
                new DomainErrorType.InvalidTransition(FromState: "Empty", ToState: "Empty"),
                cart.ToString()!,
                "빈 카트에서 아이템을 제거할 수 없습니다")),
        Paid => Fin.Fail<ShoppingCart>(
            DomainError.For<ShoppingCart>(
                new DomainErrorType.InvalidTransition(FromState: "Paid", ToState: "Empty"),
                cart.ToString()!,
                "결제 완료된 카트의 아이템을 제거할 수 없습니다")),
        _ => throw new InvalidOperationException()
    };
}
