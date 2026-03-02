using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace NamingRules.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(NamingRules.Domains.OrderService).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(NamingRules.Domains.OrderService).Namespace!;

    protected static readonly string DtoNamespace =
        typeof(NamingRules.Dtos.OrderDto).Namespace!;

    protected static readonly string RepositoryNamespace =
        typeof(NamingRules.Repositories.IOrderRepository).Namespace!;
}

[Trait("Part1-ClassValidator-Basics", "NamingRules")]
public sealed class NamingArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void ServiceClasses_ShouldEndWith_Service()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameEndingWith("Service")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNameEndsWith("Service"),
                verbose: true)
            .ThrowIfAnyFailures("Service Naming Rule");
    }

    [Fact]
    public void EventClasses_ShouldEndWith_Event()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameEndingWith("Event")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNameEndsWith("Event"),
                verbose: true)
            .ThrowIfAnyFailures("Event Naming Rule");
    }

    [Fact]
    public void DtoClasses_ShouldEndWith_Dto()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DtoNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNameEndsWith("Dto"),
                verbose: true)
            .ThrowIfAnyFailures("DTO Naming Rule");
    }

    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        ArchRuleDefinition.Interfaces()
            .That()
            .ResideInNamespace(RepositoryNamespace)
            .ValidateAllInterfaces(Architecture, @interface => @interface
                .RequireNameStartsWith("I"),
                verbose: true)
            .ThrowIfAnyFailures("Interface Naming Rule");
    }

    [Fact]
    public void RepositoryInterfaces_ShouldMatch_RepositoryPattern()
    {
        ArchRuleDefinition.Interfaces()
            .That()
            .ResideInNamespace(RepositoryNamespace)
            .ValidateAllInterfaces(Architecture, @interface => @interface
                .RequireNameMatching(".*Repository$"),
                verbose: true)
            .ThrowIfAnyFailures("Repository Naming Rule");
    }
}
