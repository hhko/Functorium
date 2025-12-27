using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyBindCombinedValidation.ValueObjects;

// 1. public sealed 클래스 선언 - ValueObject 상속
public sealed class OrderInfo : ValueObject
{
    // 1.1 readonly 속성 선언 - 불변성 보장
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public decimal OrderAmount { get; }
    public decimal FinalAmount { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private OrderInfo(string customerName, string customerEmail, decimal orderAmount, decimal finalAmount) =>
        (CustomerName, CustomerEmail, OrderAmount, FinalAmount) = (customerName, customerEmail, orderAmount, finalAmount);

    // 3. Public Create 메서드
    public static Fin<OrderInfo> Create(string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
        CreateFromValidation(
            Validate(customerName, customerEmail, orderAmountInput, discountInput),
            validValues => new OrderInfo(
                validValues.CustomerName,
                validValues.CustomerEmail,
                validValues.OrderAmount,
                validValues.FinalAmount));

    // 4. Internal CreateFromValidated 메서드
    internal static OrderInfo CreateFromValidated((string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount) validatedValues) =>
        new OrderInfo(validatedValues.CustomerName, validatedValues.CustomerEmail, validatedValues.OrderAmount, validatedValues.FinalAmount);

    // 5. Public Validate 메서드 - 혼합 검증 패턴 구현 (Apply + Bind)
    public static Validation<Error, (string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount)> Validate(
        string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
        // 5.1 독립 검증 (Apply) - 기본 정보들을 병렬로 검증
        (ValidateCustomerName(customerName), ValidateCustomerEmail(customerEmail))
            .Apply((validName, validEmail) => (validName, validEmail))
            .As()
            // 5.2 의존 검증 (Bind) - 금액 정보들을 순차적으로 검증
            .Bind(_ => ValidateOrderAmount(orderAmountInput))
            .Bind(_ => ValidateFinalAmount(orderAmountInput, discountInput))
            .Map(_ => (customerName: customerName, 
                       customerEmail: customerEmail, 
                       orderAmount: decimal.Parse(orderAmountInput), 
                       finalAmount: decimal.Parse(orderAmountInput) - decimal.Parse(discountInput)));

    // 5.1 고객명 검증 (독립)
    private static Validation<Error, string> ValidateCustomerName(string customerName) =>
        !string.IsNullOrWhiteSpace(customerName) && customerName.Length >= 2
            ? customerName
            : DomainErrors.CustomerNameTooShort(customerName);

    // 5.2 고객 이메일 검증 (독립)
    private static Validation<Error, string> ValidateCustomerEmail(string customerEmail) =>
        !string.IsNullOrWhiteSpace(customerEmail) && customerEmail.Contains("@")
            ? customerEmail
            : DomainErrors.CustomerEmailMissingAt(customerEmail);

    // 5.3 주문 금액 검증 (의존)
    private static Validation<Error, decimal> ValidateOrderAmount(string orderAmountInput) =>
        decimal.TryParse(orderAmountInput, out var orderAmount) && orderAmount > 0
            ? orderAmount
            : DomainErrors.OrderAmountNotPositive(orderAmountInput);

    // 5.4 최종 금액 검증 (의존)
    private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
        decimal.TryParse(orderAmountInput, out var orderAmount) && 
        decimal.TryParse(discountInput, out var discount) && 
        discount >= 0 && discount <= orderAmount
            ? orderAmount - discount
            : DomainErrors.DiscountAmountExceedsOrder(orderAmountInput, discountInput);

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CustomerName;
        yield return CustomerEmail;
        yield return OrderAmount;
        yield return FinalAmount;
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateCustomerName 메서드와 1:1 매핑되는 에러 - 비즈니스 규칙: 고객명은 최소 2자 이상이어야 함
        public static Error CustomerNameTooShort(string customerName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderInfo)}.{nameof(CustomerNameTooShort)}",
                errorCurrentValue: customerName,
                errorMessage: "");

        // ValidateCustomerEmail 메서드와 1:1 매핑되는 에러 - 비즈니스 규칙: 고객 이메일은 @ 기호가 포함되어야 함
        public static Error CustomerEmailMissingAt(string customerEmail) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderInfo)}.{nameof(CustomerEmailMissingAt)}",
                errorCurrentValue: customerEmail,
                errorMessage: "");

        // ValidateOrderAmount 메서드와 1:1 매핑되는 에러 - 비즈니스 규칙: 주문 금액은 양수여야 함
        public static Error OrderAmountNotPositive(string orderAmountInput) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderInfo)}.{nameof(OrderAmountNotPositive)}",
                errorCurrentValue: orderAmountInput,
                errorMessage: "");

        // ValidateFinalAmount 메서드와 1:1 매핑되는 에러 - 비즈니스 규칙: 할인 금액은 주문 금액을 초과할 수 없음
        public static Error DiscountAmountExceedsOrder(string orderAmountInput, string discountInput) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderInfo)}.{nameof(DiscountAmountExceedsOrder)}",
                errorCurrentValue: $"{orderAmountInput}:{discountInput}",
                errorMessage: "");
    }
}
