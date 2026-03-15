using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Domains.Entities;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class EntityArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void AggregateRoot_ShouldBe_PublicSealedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed()
                .RequireNotStatic(),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Visibility Rule");
    }

    [Fact]
    public void AggregateRoot_ShouldInherit_AggregateRootBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(AggregateRoot<>)),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Inheritance Rule");
    }

    [Fact]
    public void AggregateRoot_ShouldHave_CreateAndCreateFromValidated()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod(IEntity.CreateMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IEntity.CreateFromValidatedMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Factory Method Rule");
    }

    [Fact]
    public void Entity_ShouldBe_PublicSealedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .And().AreNotAssignableTo(typeof(AggregateRoot<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed()
                .RequireNotStatic(),
                verbose: true)
            .ThrowIfAnyFailures("Entity Visibility Rule");
    }

    [Fact]
    public void Entity_ShouldHave_CreateAndCreateFromValidated()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .And().AreNotAssignableTo(typeof(AggregateRoot<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod(IEntity.CreateMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IEntity.CreateFromValidatedMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("Entity Factory Method Rule");
    }

    [Fact]
    public void AggregateRoot_ShouldHave_GenerateEntityIdAttribute()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAttribute("GenerateEntityId"),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot GenerateEntityId Attribute Rule");
    }

    [Fact]
    public void AggregateRoot_ShouldHave_AllPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Private Constructors Rule");
    }

    [Fact]
    public void Entity_ShouldHave_AllPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .And().AreNotAssignableTo(typeof(AggregateRoot<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("Entity Private Constructors Rule");
    }
}
