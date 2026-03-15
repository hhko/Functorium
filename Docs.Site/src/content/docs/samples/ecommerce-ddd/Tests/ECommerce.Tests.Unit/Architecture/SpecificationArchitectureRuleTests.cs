using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using Functorium.Domains.Specifications;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace ECommerce.Tests.Unit.Architecture;

public sealed class SpecificationArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void Specification_ShouldBe_PublicSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Specification Visibility Rule");
    }

    [Fact]
    public void Specification_ShouldInherit_SpecificationBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(Specification<>)),
                verbose: true)
            .ThrowIfAnyFailures("Specification Inheritance Rule");
    }

    [Fact]
    public void Specification_ShouldResideIn_DomainLayer()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .Should().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .Check(Architecture);
    }
}
