using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace FirstArchitectureTest.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(FirstArchitectureTest.Domains.Employee).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(FirstArchitectureTest.Domains.Employee).Namespace!;
}

[Trait("Part1-ClassValidator-Basics", "FirstArchitectureTest")]
public sealed class EmployeeArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void DomainClasses_ShouldBe_Public()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Class Visibility Rule");
    }

    [Fact]
    public void DomainClasses_ShouldBe_Sealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Class Sealed Rule");
    }

    [Fact]
    public void DomainClasses_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Class Public Sealed Rule");
    }
}
