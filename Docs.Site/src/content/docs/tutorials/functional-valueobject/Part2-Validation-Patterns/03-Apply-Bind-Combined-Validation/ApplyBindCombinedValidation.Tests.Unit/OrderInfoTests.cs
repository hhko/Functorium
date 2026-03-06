using ApplyBindCombinedValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyBindCombinedValidation.Tests.Unit;

/// <summary>
/// OrderInfo 값 객체의 Apply + Bind 혼합 검증 패턴 테스트
///
/// 학습 목표:
/// 1. Apply와 Bind를 혼합한 검증 패턴 이해
/// 2. 독립 검증(Apply)과 의존 검증(Bind)의 조합 방법 학습
/// 3. 혼합 패턴에서의 에러 수집 동작 검증
/// </summary>
[Trait("Part2-Validation", "03-Apply-Bind-Combined")]
public class OrderInfoTests
{
    // 테스트 시나리오: 모든 필드가 유효할 때 OrderInfo 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Arrange
        string customerName = "John Doe";
        string customerEmail = "john@example.com";
        string orderAmountInput = "100.00";
        string discountInput = "10.00";

        // Act
        Fin<OrderInfo> actual = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: order =>
            {
                order.CustomerName.ShouldBe(customerName);
                order.CustomerEmail.ShouldBe(customerEmail);
                order.OrderAmount.ShouldBe(100.00m);
                order.FinalAmount.ShouldBe(90.00m);  // 100 - 10
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 고객명이 너무 짧을 때 실패 (Apply 독립 검증)
    [Fact]
    public void Create_ReturnsFail_WhenCustomerNameTooShort()
    {
        // Arrange
        string customerName = "J";  // 2자 미만
        string customerEmail = "john@example.com";
        string orderAmountInput = "100.00";
        string discountInput = "10.00";

        // Act
        Fin<OrderInfo> actual = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 이메일에 @ 기호가 없을 때 실패 (Apply 독립 검증)
    [Fact]
    public void Create_ReturnsFail_WhenCustomerEmailMissingAt()
    {
        // Arrange
        string customerName = "John Doe";
        string customerEmail = "johnexample.com";  // @ 없음
        string orderAmountInput = "100.00";
        string discountInput = "10.00";

        // Act
        Fin<OrderInfo> actual = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 주문 금액이 양수가 아닐 때 실패 (Bind 의존 검증)
    [Fact]
    public void Create_ReturnsFail_WhenOrderAmountNotPositive()
    {
        // Arrange
        string customerName = "John Doe";
        string customerEmail = "john@example.com";
        string orderAmountInput = "-50.00";  // 음수
        string discountInput = "10.00";

        // Act
        Fin<OrderInfo> actual = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 할인 금액이 주문 금액을 초과할 때 실패 (Bind 의존 검증)
    [Fact]
    public void Create_ReturnsFail_WhenDiscountExceedsOrderAmount()
    {
        // Arrange
        string customerName = "John Doe";
        string customerEmail = "john@example.com";
        string orderAmountInput = "100.00";
        string discountInput = "150.00";  // 주문 금액 초과

        // Act
        Fin<OrderInfo> actual = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: Apply 부분의 에러는 병렬로 수집됨
    [Fact]
    public void Validate_CollectsApplyErrors_WhenBothCustomerFieldsInvalid()
    {
        // Arrange - 고객명과 이메일 모두 유효하지 않음 (Apply 부분)
        string customerName = "J";            // 2자 미만
        string customerEmail = "invalid";     // @ 없음
        string orderAmountInput = "100.00";   // 유효
        string discountInput = "10.00";       // 유효

        // Act
        var actual = OrderInfo.Validate(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert - Apply 부분에서 2개 에러 수집
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(2));
    }

    // 테스트 시나리오: 할인이 0일 때 최종 금액이 주문 금액과 동일
    [Fact]
    public void Create_ReturnsCorrectFinalAmount_WhenDiscountIsZero()
    {
        // Arrange
        string customerName = "John Doe";
        string customerEmail = "john@example.com";
        string orderAmountInput = "100.00";
        string discountInput = "0";

        // Act
        Fin<OrderInfo> actual = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: order =>
            {
                order.FinalAmount.ShouldBe(100.00m);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string customerName = "John Doe";
        string customerEmail = "john@example.com";
        string orderAmountInput = "100.00";
        string discountInput = "10.00";

        // Act
        Fin<OrderInfo> actual1 = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);
        Fin<OrderInfo> actual2 = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }
}
