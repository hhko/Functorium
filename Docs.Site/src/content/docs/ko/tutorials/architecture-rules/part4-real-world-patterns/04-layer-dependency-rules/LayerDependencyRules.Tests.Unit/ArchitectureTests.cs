using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LayerDependencyRules.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(LayerDependencyRules.Domains.Product).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(LayerDependencyRules.Domains.Product).Namespace!;

    protected static readonly string ApplicationNamespace =
        typeof(LayerDependencyRules.Applications.GetProduct).Namespace!;

    protected static readonly string AdapterNamespace =
        typeof(LayerDependencyRules.Adapters.Persistence.ProductRepository).Namespace!
            .Replace(".Persistence", "");
}

[Trait("Part4-Real-World-Patterns", "LayerDependencyRules")]
public sealed class LayerDependencyArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .ResideInNamespace(DomainNamespace)
            .Should()
            .NotDependOnAnyTypesThat()
            .ResideInNamespace(ApplicationNamespace)
            .Check(Architecture);
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

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .Should()
            .NotDependOnAnyTypesThat()
            .ResideInNamespace(AdapterNamespace)
            .Check(Architecture);
    }
}

[Trait("Part4-Real-World-Patterns", "LayerDependencyRules")]
public sealed class DomainClassArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void DomainClasses_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .DoNotResideInNamespace(DomainNamespace + ".Ports")
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Class Rule");
    }

    [Fact]
    public void DomainClasses_ShouldHave_AllPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .DoNotResideInNamespace(DomainNamespace + ".Ports")
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Class Private Constructor Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "LayerDependencyRules")]
public sealed class ApplicationClassArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void ApplicationClasses_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Application Class Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "LayerDependencyRules")]
public sealed class AdapterClassArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void AdapterClasses_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(AdapterNamespace)
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Adapter Class Rule");
    }
}
