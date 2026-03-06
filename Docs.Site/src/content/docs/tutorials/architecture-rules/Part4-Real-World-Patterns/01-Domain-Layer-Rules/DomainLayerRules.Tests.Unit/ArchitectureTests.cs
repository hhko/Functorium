using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace DomainLayerRules.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(DomainLayerRules.Domains.Order).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(DomainLayerRules.Domains.Order).Namespace!;
}

[Trait("Part4-Real-World-Patterns", "DomainLayerRules")]
public sealed class EntityArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Entities_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.Entity<>))
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Entity Public Sealed Rule");
    }

    [Fact]
    public void Entities_ShouldHave_CreateFactory()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.Entity<>))
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m.RequireStatic())
                .RequireMethod("CreateFromValidated", m => m.RequireStatic()),
                verbose: true)
            .ThrowIfAnyFailures("Entity Factory Method Rule");
    }

    [Fact]
    public void Entities_ShouldHave_AllPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.Entity<>))
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("Entity Private Constructor Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "DomainLayerRules")]
public sealed class ValueObjectArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void ValueObjects_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.IValueObject))
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Value Object Public Sealed Rule");
    }

    [Fact]
    public void ValueObjects_ShouldBe_Immutable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.IValueObject))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImmutable(),
                verbose: true)
            .ThrowIfAnyFailures("Value Object Immutability Rule");
    }

    [Fact]
    public void ValueObjects_ShouldImplement_IValueObject()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.IValueObject))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImplements(typeof(DomainLayerRules.Domains.IValueObject)),
                verbose: true)
            .ThrowIfAnyFailures("Value Object Interface Rule");
    }

    [Fact]
    public void ValueObjects_ShouldHave_CreateFactory()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.IValueObject))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m.RequireStatic()),
                verbose: true)
            .ThrowIfAnyFailures("Value Object Factory Method Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "DomainLayerRules")]
public sealed class DomainEventArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void DomainEvents_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.DomainEvent))
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Event Public Sealed Rule");
    }

    [Fact]
    public void DomainEvents_ShouldHave_CreateFactory()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.DomainEvent))
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m.RequireStatic()),
                verbose: true)
            .ThrowIfAnyFailures("Domain Event Factory Method Rule");
    }

    [Fact]
    public void DomainEvents_ShouldInherit_DomainEvent()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreAssignableTo(typeof(DomainLayerRules.Domains.DomainEvent))
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(DomainLayerRules.Domains.DomainEvent)),
                verbose: true)
            .ThrowIfAnyFailures("Domain Event Inheritance Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "DomainLayerRules")]
public sealed class DomainServiceArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void DomainServices_ShouldBe_PublicAndStatic()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameEndingWith("Service")
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireStatic(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Service Public Static Rule");
    }
}
