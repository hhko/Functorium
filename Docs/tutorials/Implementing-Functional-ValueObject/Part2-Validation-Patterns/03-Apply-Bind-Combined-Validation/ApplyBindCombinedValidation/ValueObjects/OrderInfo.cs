using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyBindCombinedValidation.ValueObjects;

/// <summary>
/// OrderInfo 값 객체 - 혼합 검증(Apply + Bind) 패턴 예제
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class OrderInfo : ValueObject
{
    public sealed record DiscountAmountExceedsOrder : DomainErrorType.Custom;
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public decimal OrderAmount { get; }
    public decimal FinalAmount { get; }

    private OrderInfo(string customerName, string customerEmail, decimal orderAmount, decimal finalAmount) =>
        (CustomerName, CustomerEmail, OrderAmount, FinalAmount) = (customerName, customerEmail, orderAmount, finalAmount);

    public static Fin<OrderInfo> Create(string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
        CreateFromValidation(
            Validate(customerName, customerEmail, orderAmountInput, discountInput),
            v => new OrderInfo(v.CustomerName, v.CustomerEmail, v.OrderAmount, v.FinalAmount));

    public static OrderInfo CreateFromValidated((string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount) v) =>
        new(v.CustomerName, v.CustomerEmail, v.OrderAmount, v.FinalAmount);

    // 혼합 검증 - Apply(병렬) + Bind(순차) 패턴
    public static Validation<Error, (string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount)> Validate(
        string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
        // 독립 검증 (Apply) - 기본 정보들을 병렬로 검증
        (ValidateCustomerName(customerName), ValidateCustomerEmail(customerEmail))
            .Apply((n, e) => (n, e))
            // 의존 검증 (Bind) - 금액 정보들을 순차적으로 검증
            .Bind(_ => ValidateOrderAmount(orderAmountInput))
            .Bind(_ => ValidateFinalAmount(orderAmountInput, discountInput))
            .Map(_ => (customerName, customerEmail,
                       decimal.Parse(orderAmountInput),
                       decimal.Parse(orderAmountInput) - decimal.Parse(discountInput)));

    private static Validation<Error, string> ValidateCustomerName(string customerName) =>
        !string.IsNullOrWhiteSpace(customerName) && customerName.Length >= 2
            ? customerName
            : DomainError.For<OrderInfo>(new DomainErrorType.TooShort(), customerName,
                $"Customer name is too short. Minimum length is 2 characters. Current value: '{customerName}'");

    private static Validation<Error, string> ValidateCustomerEmail(string customerEmail) =>
        !string.IsNullOrWhiteSpace(customerEmail) && customerEmail.Contains("@")
            ? customerEmail
            : DomainError.For<OrderInfo>(new DomainErrorType.InvalidFormat(), customerEmail,
                $"Customer email is missing '@' symbol. Current value: '{customerEmail}'");

    private static Validation<Error, decimal> ValidateOrderAmount(string orderAmountInput) =>
        decimal.TryParse(orderAmountInput, out var orderAmount) && orderAmount > 0
            ? orderAmount
            : DomainError.For<OrderInfo>(new DomainErrorType.NotPositive(), orderAmountInput,
                $"Order amount must be a positive number. Current value: '{orderAmountInput}'");

    private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
        decimal.TryParse(orderAmountInput, out var orderAmount) &&
        decimal.TryParse(discountInput, out var discount) &&
        discount >= 0 && discount <= orderAmount
            ? orderAmount - discount
            : DomainError.For<OrderInfo>(new DiscountAmountExceedsOrder(), $"{orderAmountInput}:{discountInput}",
                $"Discount amount cannot exceed order amount. Order: '{orderAmountInput}', Discount: '{discountInput}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CustomerName;
        yield return CustomerEmail;
        yield return OrderAmount;
        yield return FinalAmount;
    }
}
