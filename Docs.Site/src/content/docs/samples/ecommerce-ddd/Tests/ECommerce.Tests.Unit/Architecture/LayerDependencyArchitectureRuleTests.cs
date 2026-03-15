using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ECommerce.Tests.Unit.Architecture;

public sealed class LayerDependencyArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .ResideInNamespace(DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ApplicationNamespace)
            .Check(Architecture);
    }
}
