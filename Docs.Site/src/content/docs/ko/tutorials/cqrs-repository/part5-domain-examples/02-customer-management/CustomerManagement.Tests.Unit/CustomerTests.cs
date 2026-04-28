using CustomerManagement;

namespace CustomerManagement.Tests.Unit;

public sealed class CustomerTests
{
    // ─── Create ─────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsSucc()
    {
        var result = Customer.Create("홍길동", "hong@example.com", 1_000_000m);

        result.IsSucc.ShouldBeTrue();
        var customer = result.ThrowIfFail();
        customer.Name.ShouldBe("홍길동");
        customer.Email.ShouldBe("hong@example.com");
        customer.CreditLimit.ShouldBe(1_000_000m);
    }

    [Fact]
    public void Create_EmptyName_ReturnsFail()
    {
        var result = Customer.Create("", "hong@example.com");
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_EmptyEmail_ReturnsFail()
    {
        var result = Customer.Create("홍길동", "");
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_NegativeCreditLimit_ReturnsFail()
    {
        var result = Customer.Create("홍길동", "hong@example.com", -1m);
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_RaisesCustomerCreatedEvent()
    {
        var customer = Customer.Create("홍길동", "hong@example.com").ThrowIfFail();

        customer.DomainEvents.Count.ShouldBe(1);
        customer.DomainEvents[0].ShouldBeOfType<Customer.CustomerCreatedEvent>();
    }

    // ─── UpdateCreditLimit ──────────────────────────────

    [Fact]
    public void UpdateCreditLimit_ValidAmount_ReturnsSucc()
    {
        var customer = Customer.Create("홍길동", "hong@example.com", 100_000m).ThrowIfFail();

        var result = customer.UpdateCreditLimit(500_000m);

        result.IsSucc.ShouldBeTrue();
        customer.CreditLimit.ShouldBe(500_000m);
        customer.UpdatedAt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void UpdateCreditLimit_NegativeAmount_ReturnsFail()
    {
        var customer = Customer.Create("홍길동", "hong@example.com").ThrowIfFail();

        var result = customer.UpdateCreditLimit(-1m);

        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void UpdateCreditLimit_RaisesCreditLimitUpdatedEvent()
    {
        var customer = Customer.Create("홍길동", "hong@example.com", 100_000m).ThrowIfFail();

        customer.UpdateCreditLimit(500_000m);

        customer.DomainEvents.Count.ShouldBe(2); // Created + Updated
        var evt = customer.DomainEvents[1].ShouldBeOfType<Customer.CreditLimitUpdatedEvent>();
        evt.OldLimit.ShouldBe(100_000m);
        evt.NewLimit.ShouldBe(500_000m);
    }

    // ─── ChangeEmail ────────────────────────────────────

    [Fact]
    public void ChangeEmail_ValidEmail_ReturnsSucc()
    {
        var customer = Customer.Create("홍길동", "old@example.com").ThrowIfFail();

        var result = customer.ChangeEmail("new@example.com");

        result.IsSucc.ShouldBeTrue();
        customer.Email.ShouldBe("new@example.com");
    }

    [Fact]
    public void ChangeEmail_EmptyEmail_ReturnsFail()
    {
        var customer = Customer.Create("홍길동", "old@example.com").ThrowIfFail();

        var result = customer.ChangeEmail("");

        result.IsFail.ShouldBeTrue();
    }

    // ─── Specification ──────────────────────────────────

    [Fact]
    public void CustomerEmailSpec_MatchesExactEmail()
    {
        var customer = Customer.Create("홍길동", "hong@example.com").ThrowIfFail();
        var spec = new CustomerEmailSpec("hong@example.com");

        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void CustomerEmailSpec_CaseInsensitive()
    {
        var customer = Customer.Create("홍길동", "Hong@Example.COM").ThrowIfFail();
        var spec = new CustomerEmailSpec("hong@example.com");

        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void CustomerNameSpec_MatchesPartialName()
    {
        var customer = Customer.Create("홍길동", "hong@example.com").ThrowIfFail();
        var spec = new CustomerNameSpec("길동");

        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void CustomerNameSpec_NoMatch_ReturnsFalse()
    {
        var customer = Customer.Create("홍길동", "hong@example.com").ThrowIfFail();
        var spec = new CustomerNameSpec("이순신");

        spec.IsSatisfiedBy(customer).ShouldBeFalse();
    }
}
