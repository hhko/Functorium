using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LayeredArch.Tests.Unit.Architecture;

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
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void PresentationAdapter_ShouldNotDependOn_OtherAdapters()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void PersistenceAdapter_ShouldNotDependOn_OtherAdapters()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void InfrastructureAdapter_ShouldNotDependOn_OtherAdapters()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }
}
