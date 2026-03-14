using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Unions;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

// # UnionValueObject 기반 타입 테스트
//
// UnionValueObject, UnionValueObject<TSelf>, UnreachableCaseException 검증
//
// ## 테스트 시나리오
//
// ### 1. UnionValueObject — IUnionValueObject 마커 인터페이스 구현
// ### 2. UnionValueObject<TSelf> — TransitionFrom 성공/실패
// ### 3. UnreachableCaseException — 메시지 포맷
//
[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public sealed class UnionValueObjectTests
{
    // 테스트용 순수 데이터 유니온
    private abstract record TestShape : UnionValueObject
    {
        public sealed record Circle(double Radius) : TestShape;
        public sealed record Rectangle(double Width, double Height) : TestShape;
        private TestShape() { }
    }

    // 테스트용 상태 기계 유니온
    private abstract record TestOrderState : UnionValueObject<TestOrderState>
    {
        public sealed record Pending(string OrderId) : TestOrderState;
        public sealed record Confirmed(string OrderId, DateTime ConfirmedAt) : TestOrderState;
        private TestOrderState() { }

        public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
            TransitionFrom<Pending, Confirmed>(
                p => new Confirmed(p.OrderId, confirmedAt));
    }

    #region 1. UnionValueObject — IUnionValueObject 마커 인터페이스 구현

    [Fact]
    public void UnionValueObject_Implements_IUnionValueObject()
    {
        // Arrange
        TestShape sut = new TestShape.Circle(5.0);

        // Assert
        sut.ShouldBeAssignableTo<IUnionValueObject>();
    }

    [Fact]
    public void UnionValueObject_Implements_IValueObject()
    {
        // Arrange
        TestShape sut = new TestShape.Circle(5.0);

        // Assert
        sut.ShouldBeAssignableTo<IValueObject>();
    }

    [Fact]
    public void UnionValueObject_RecordEquality_Works()
    {
        // Arrange
        var a = new TestShape.Circle(5.0);
        var b = new TestShape.Circle(5.0);

        // Assert
        a.ShouldBe(b);
    }

    [Fact]
    public void UnionValueObject_RecordInequality_Works()
    {
        // Arrange
        TestShape a = new TestShape.Circle(5.0);
        TestShape b = new TestShape.Rectangle(3.0, 4.0);

        // Assert
        a.ShouldNotBe(b);
    }

    #endregion

    #region 2. UnionValueObject<TSelf> — TransitionFrom 성공/실패

    [Fact]
    public void TransitionFrom_ReturnsSuccess_WhenSourceMatches()
    {
        // Arrange
        TestOrderState sut = new TestOrderState.Pending("ORD-001");
        var confirmedAt = new DateTime(2024, 1, 15);

        // Act
        var actual = sut.Confirm(confirmedAt);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var confirmed = actual.ThrowIfFail();
        confirmed.OrderId.ShouldBe("ORD-001");
        confirmed.ConfirmedAt.ShouldBe(confirmedAt);
    }

    [Fact]
    public void TransitionFrom_ReturnsFail_WhenSourceDoesNotMatch()
    {
        // Arrange
        TestOrderState sut = new TestOrderState.Confirmed("ORD-001", new DateTime(2024, 1, 15));

        // Act
        var actual = sut.Confirm(new DateTime(2024, 2, 1));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void TransitionFrom_FailError_ContainsInvalidTransition()
    {
        // Arrange
        TestOrderState sut = new TestOrderState.Confirmed("ORD-001", new DateTime(2024, 1, 15));

        // Act
        var actual = sut.Confirm(new DateTime(2024, 2, 1));

        // Assert
        actual.IsFail.ShouldBeTrue();
        var error = (Error)actual;
        error.Message.ShouldContain("Invalid transition from Confirmed to Confirmed");
    }

    [Fact]
    public void TransitionFrom_StateMachine_Implements_IUnionValueObject()
    {
        // Arrange
        TestOrderState sut = new TestOrderState.Pending("ORD-001");

        // Assert
        sut.ShouldBeAssignableTo<IUnionValueObject>();
    }

    #endregion

    #region 3. UnreachableCaseException — 메시지 포맷

    [Fact]
    public void UnreachableCaseException_ContainsTypeFullName()
    {
        // Arrange & Act
        var ex = new UnreachableCaseException("test");

        // Assert
        ex.Message.ShouldContain("System.String");
    }

    [Fact]
    public void UnreachableCaseException_IsInvalidOperationException()
    {
        // Arrange & Act
        var ex = new UnreachableCaseException(42);

        // Assert
        ex.ShouldBeAssignableTo<InvalidOperationException>();
    }

    #endregion
}
