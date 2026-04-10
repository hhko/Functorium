using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AiGovernance.Tests.Unit.Architecture;

public sealed class LayerDependencyArchitectureRuleTests
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .WithoutRequiringPositiveResults()
            .Check(ArchitectureTestBase.Architecture);
    }
}
