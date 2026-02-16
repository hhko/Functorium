using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LayeredArch.Tests.Unit.Architecture;

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

    [Fact]
    public void DomainLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(InfrastructureNamespace)
            .Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(InfrastructureNamespace)
            .Check(Architecture);
    }
}
