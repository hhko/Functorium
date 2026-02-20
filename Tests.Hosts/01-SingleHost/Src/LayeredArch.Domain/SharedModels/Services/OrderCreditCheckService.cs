using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Domain.SharedModels.Services;

/// <summary>
/// 주문 신용 한도 검증 도메인 서비스.
/// Customer와 Order 간의 교차 Aggregate 비즈니스 규칙을 구현합니다.
/// </summary>
public sealed class OrderCreditCheckService : IDomainService
{
    /// <summary>
    /// 주문 금액이 고객의 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new Custom("CreditLimitExceeded"),
                customer.Id.ToString(),
                $"Order amount {(decimal)orderAmount} exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }

    /// <summary>
    /// 기존 주문들과 신규 주문을 합산하여 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimitWithExistingOrders(
        Customer customer,
        Seq<Order> existingOrders,
        Money newOrderAmount)
    {
        var totalExisting = Money.Sum(existingOrders.Map(o => o.TotalAmount));
        var totalWithNew = totalExisting.Add(newOrderAmount);

        if (totalWithNew > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new Custom("CreditLimitExceeded"),
                customer.Id.ToString(),
                $"Total order amount {(decimal)totalWithNew} (existing: {(decimal)totalExisting} + new: {(decimal)newOrderAmount}) exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }
}
