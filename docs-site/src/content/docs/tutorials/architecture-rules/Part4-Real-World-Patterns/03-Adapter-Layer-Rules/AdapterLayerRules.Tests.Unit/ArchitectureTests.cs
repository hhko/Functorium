using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AdapterLayerRules.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(AdapterLayerRules.Domains.Order).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(AdapterLayerRules.Domains.Order).Namespace!;

    protected static readonly string PortNamespace =
        typeof(AdapterLayerRules.Domains.Ports.IOrderRepository).Namespace!;

    protected static readonly string AdapterNamespace =
        typeof(AdapterLayerRules.Adapters.Persistence.OrderRepository).Namespace!
            .Replace(".Persistence", "");
}

[Trait("Part4-Real-World-Patterns", "AdapterLayerRules")]
public sealed class PortArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void PortInterfaces_ShouldHave_NameStartingWithI()
    {
        Interfaces()
            .That()
            .ResideInNamespace(PortNamespace)
            .ValidateAllInterfaces(Architecture, @interface => @interface
                .RequireNameStartsWith("I"),
                verbose: true)
            .ThrowIfAnyFailures("Port Interface Naming Rule");
    }

    [Fact]
    public void PortInterfaces_ShouldResideIn_DomainsPortsNamespace()
    {
        Interfaces()
            .That()
            .ResideInNamespace(PortNamespace)
            .Should()
            .ResideInNamespace(PortNamespace)
            .Check(Architecture);
    }
}

[Trait("Part4-Real-World-Patterns", "AdapterLayerRules")]
public sealed class AdapterArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Adapters_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(AdapterNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Adapter Public Sealed Rule");
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(DomainNamespace)
            .Should()
            .NotDependOnAnyTypesThat()
            .ResideInNamespace(AdapterNamespace)
            .Check(Architecture);
    }
}
