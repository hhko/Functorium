using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace ArchitectureTestSuites.Tests.Unit;

// --- Step 1: DomainArchitectureTestSuite 상속 ---

[Trait("Part4-Real-World-Patterns", "ArchitectureTestSuites")]
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader()
            .LoadAssemblies(typeof(DomainLayerRules.Domains.Order).Assembly)
            .Build();

    protected override string DomainNamespace { get; } =
        typeof(DomainLayerRules.Domains.Order).Namespace!;

    // --- Step 2: 프로젝트별 커스텀 규칙 추가 ---

    [Fact]
    public void AggregateRoot_ShouldInherit_AggregateRootBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(DomainLayerRules.Domains.AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(DomainLayerRules.Domains.AggregateRoot<>)),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Inheritance Rule");
    }
}
