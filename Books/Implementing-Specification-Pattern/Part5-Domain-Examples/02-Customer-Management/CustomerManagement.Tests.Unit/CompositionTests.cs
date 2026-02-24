using CustomerManagement.Domain;
using CustomerManagement.Domain.Specifications;
using CustomerManagement.Domain.ValueObjects;

namespace CustomerManagement.Tests.Unit;

public class CompositionTests
{
    private static readonly Customer _활성_김철수 = new(
        CustomerId.New(), new CustomerName("김철수"), new Email("chulsoo@example.com"), IsActive: true);

    private static readonly Customer _비활성_박지민 = new(
        CustomerId.New(), new CustomerName("박지민"), new Email("jimin@example.com"), IsActive: false);

    private static readonly Customer _활성_최수진 = new(
        CustomerId.New(), new CustomerName("최수진"), new Email("soojin@company.co.kr"), IsActive: true);

    [Fact]
    public void And_ShouldCombine_ActiveAndEmail()
    {
        // Arrange
        var spec = new ActiveCustomerSpec()
            & new CustomerEmailSpec(new Email("chulsoo@example.com"));

        // Act & Assert
        spec.IsSatisfiedBy(_활성_김철수).ShouldBeTrue();
        spec.IsSatisfiedBy(_비활성_박지민).ShouldBeFalse();
        spec.IsSatisfiedBy(_활성_최수진).ShouldBeFalse();
    }

    [Fact]
    public void And_ShouldCombine_ActiveAndNameContains()
    {
        // Arrange: 활성 AND 이름에 '수' 포함
        var spec = new ActiveCustomerSpec()
            & new CustomerNameContainsSpec(new CustomerName("수"));

        // Act & Assert
        spec.IsSatisfiedBy(_활성_김철수).ShouldBeTrue();
        spec.IsSatisfiedBy(_활성_최수진).ShouldBeTrue();
        spec.IsSatisfiedBy(_비활성_박지민).ShouldBeFalse();
    }

    [Fact]
    public void Not_ShouldNegate_ActiveSpec()
    {
        // Arrange: 비활성 고객
        var inactiveSpec = !new ActiveCustomerSpec();

        // Act & Assert
        inactiveSpec.IsSatisfiedBy(_비활성_박지민).ShouldBeTrue();
        inactiveSpec.IsSatisfiedBy(_활성_김철수).ShouldBeFalse();
    }

    [Fact]
    public void Or_ShouldCombine_EmailOrNameContains()
    {
        // Arrange: 이메일 일치 OR 이름에 '수' 포함
        var spec = new CustomerEmailSpec(new Email("jimin@example.com"))
            | new CustomerNameContainsSpec(new CustomerName("수"));

        // Act & Assert
        spec.IsSatisfiedBy(_활성_김철수).ShouldBeTrue();
        spec.IsSatisfiedBy(_비활성_박지민).ShouldBeTrue();
        spec.IsSatisfiedBy(_활성_최수진).ShouldBeTrue();
    }
}
