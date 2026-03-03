using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using InheritanceAndInterface.Domains;
using Xunit;

namespace InheritanceAndInterface.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(Product).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(Product).Namespace!;

    protected static readonly string ServiceNamespace =
        typeof(InheritanceAndInterface.Services.ProductRepository).Namespace!;
}

[Trait("Part1-ClassValidator-Basics", "InheritanceAndInterface")]
public sealed class InheritanceArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Entities_ShouldInherit_EntityBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(Entity<>)),
                verbose: true)
            .ThrowIfAnyFailures("Entity Inheritance Rule");
    }

    [Fact]
    public void AggregateRoots_ShouldImplement_IAggregate()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameEndingWith("Product")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImplements(typeof(IAggregate)),
                verbose: true)
            .ThrowIfAnyFailures("Aggregate Root Rule");
    }

    [Fact]
    public void AuditableEntities_ShouldImplement_IAuditable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImplements(typeof(IAuditable)),
                verbose: true)
            .ThrowIfAnyFailures("Auditable Entity Rule");
    }
}

[Trait("Part1-ClassValidator-Basics", "InheritanceAndInterface")]
public sealed class InterfaceImplementationArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Repositories_ShouldImplement_GenericIRepository()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ServiceNamespace)
            .And()
            .HaveNameEndingWith("Repository")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImplementsGenericInterface("IRepository"),
                verbose: true)
            .ThrowIfAnyFailures("Repository Interface Rule");
    }
}
