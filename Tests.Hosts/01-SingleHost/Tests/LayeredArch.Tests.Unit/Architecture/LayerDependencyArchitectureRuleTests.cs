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
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(PresentationNamespace)
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
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(PresentationNamespace)
            .Check(Architecture);
    }

    [Fact]
    public void PresentationAdapter_ShouldNotDependOn_OtherAdapters()
    {
        Types()
            .That()
            .ResideInNamespace(PresentationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(InfrastructureNamespace)
            .Check(Architecture);
    }

    [Fact]
    public void PersistenceAdapter_ShouldNotDependOn_OtherAdapters()
    {
        Types()
            .That()
            .ResideInNamespace(PersistenceNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(PresentationNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(InfrastructureNamespace)
            .Check(Architecture);
    }

    [Fact]
    public void InfrastructureAdapter_ShouldNotDependOn_OtherAdapters()
    {
        Types()
            .That()
            .ResideInNamespace(InfrastructureNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(PresentationNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(PersistenceNamespace)
            .Check(Architecture);
    }
}
