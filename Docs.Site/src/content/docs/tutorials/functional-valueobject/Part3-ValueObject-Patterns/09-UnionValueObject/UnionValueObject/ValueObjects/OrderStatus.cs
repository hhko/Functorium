using Functorium.Domains.ValueObjects.Unions;
using LanguageExt;

namespace UnionValueObject.ValueObjects;

/// <summary>
/// Functorium의 UnionValueObject&lt;TSelf&gt;를 사용한 상태 전이 유니온.
/// TransitionFrom으로 유효한 전이만 허용합니다.
/// </summary>
public abstract record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending(string OrderId) : OrderStatus;
    public sealed record Confirmed(string OrderId, DateTime ConfirmedAt) : OrderStatus;
    private OrderStatus() { }

    public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
        TransitionFrom<Pending, Confirmed>(
            p => new Confirmed(p.OrderId, confirmedAt));
}
