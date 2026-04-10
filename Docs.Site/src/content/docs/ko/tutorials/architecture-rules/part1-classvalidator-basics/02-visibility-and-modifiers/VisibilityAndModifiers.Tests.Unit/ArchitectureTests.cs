using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace VisibilityAndModifiers.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(VisibilityAndModifiers.Domains.Order).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(VisibilityAndModifiers.Domains.Order).Namespace!;

    protected static readonly string ServiceNamespace =
        typeof(VisibilityAndModifiers.Services.OrderFormatter).Namespace!;

    protected static readonly string InternalNamespace =
        "VisibilityAndModifiers.Internal";
}

[Trait("Part1-ClassValidator-Basics", "VisibilityAndModifiers")]
public sealed class VisibilityArchitectureTests : ArchitectureTestBase
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
    public void InternalClasses_ShouldBe_Internal()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(InternalNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInternal(),
                verbose: true)
            .ThrowIfAnyFailures("Internal Class Visibility Rule");
    }
}

[Trait("Part1-ClassValidator-Basics", "VisibilityAndModifiers")]
public sealed class ModifierArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void ConcreteClasses_ShouldBe_Sealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Concrete Class Sealed Rule");
    }

    [Fact]
    public void AbstractClasses_ShouldBe_NotSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNotSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Abstract Class Not Sealed Rule");
    }

    [Fact]
    public void AbstractClasses_ShouldBe_Abstract()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAbstract(),
                verbose: true)
            .ThrowIfAnyFailures("Abstract Class Rule");
    }

    [Fact]
    public void ConcreteClasses_ShouldBe_NotAbstract()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNotAbstract(),
                verbose: true)
            .ThrowIfAnyFailures("Concrete Class Not Abstract Rule");
    }

    [Fact]
    public void ServiceClasses_ShouldBe_Static()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ServiceNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireStatic(),
                verbose: true)
            .ThrowIfAnyFailures("Service Class Static Rule");
    }

    [Fact]
    public void DomainClasses_ShouldBe_NotStatic()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNotStatic(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Class Not Static Rule");
    }
}

[Trait("Part1-ClassValidator-Basics", "VisibilityAndModifiers")]
public sealed class RecordArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void RecordTypes_ShouldBe_Record()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameEndingWith("Summary")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireRecord(),
                verbose: true)
            .ThrowIfAnyFailures("Record Type Rule");
    }

    [Fact]
    public void NonRecordClasses_ShouldBe_NotRecord()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .DoNotHaveNameEndingWith("Summary")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNotRecord(),
                verbose: true)
            .ThrowIfAnyFailures("Non-Record Class Rule");
    }
}
