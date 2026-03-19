using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using DomainLayerRules.Domains;
using Functorium.Testing.Assertions.ArchitectureRules;
using LanguageExt;
using LanguageExt.Common;
using Xunit;

namespace ArchitectureTestSuites.Tests.Unit;

public abstract class DomainArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string DomainNamespace { get; }

    // --- AggregateRoot (4) ---

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
    public void AggregateRoot_ShouldHave_CreateAndCreateFromValidated()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod("CreateFromValidated", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Factory Method Rule");
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

    // --- Entity (3) ---

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
                .RequireMethod("Create", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod("CreateFromValidated", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("Entity Factory Method Rule");
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

    // --- ValueObject (4) ---

    [Fact]
    public void ValueObject_ShouldBe_PublicSealedWithPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Visibility Rule");
    }

    [Fact]
    public void ValueObject_ShouldBe_Immutable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImmutable(),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Immutability Rule");
    }

    [Fact]
    public void ValueObject_ShouldHave_CreateFactoryMethod()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Fin<>))),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Create Method Rule");
    }

    [Fact]
    public void ValueObject_ShouldHave_ValidateMethod()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Validate", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Validation<,>))),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Validate Method Rule");
    }

    // --- DomainEvent (2) ---

    [Fact]
    public void DomainEvent_ShouldBe_SealedRecord()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(DomainEvent))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed()
                .RequireRecord(),
                verbose: true)
            .ThrowIfAnyFailures("DomainEvent Sealed Record Rule");
    }

    [Fact]
    public void DomainEvent_ShouldHave_EventSuffix()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(DomainEvent))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNameEndsWith("Event"),
                verbose: true)
            .ThrowIfAnyFailures("DomainEvent Naming Rule");
    }

    // --- Specification (3) ---

    [Fact]
    public void Specification_ShouldBe_PublicSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Specification Visibility Rule");
    }

    [Fact]
    public void Specification_ShouldInherit_SpecificationBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(Specification<>)),
                verbose: true)
            .ThrowIfAnyFailures("Specification Inheritance Rule");
    }

    [Fact]
    public void Specification_ShouldResideIn_DomainLayer()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespace(DomainNamespace)
            .Should().ResideInNamespace(DomainNamespace)
            .Check(Architecture);
    }

    // --- DomainService (5) ---

    [Fact]
    public void DomainService_ShouldBe_PublicSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Visibility Rule");
    }

    [Fact]
    public void DomainService_ShouldBe_Stateless()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoInstanceFields(),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Stateless Rule");
    }

    [Fact]
    public void DomainService_ShouldNotDependOn_IObservablePort()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoDependencyOn("IObservablePort"),
                verbose: true)
            .ThrowIfAnyFailures("DomainService No IObservablePort Dependency Rule");
    }

    [Fact]
    public void DomainService_PublicMethods_ShouldReturn_Fin()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllMethods(
                    m => m.Visibility == Visibility.Public
                         && m.IsStatic != true
                         && m.MethodForm == MethodForm.Normal,
                    method => method.RequireReturnTypeContaining("Fin")),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Public Methods Return Fin Rule");
    }

    [Fact]
    public void DomainService_ShouldNotBe_Record()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNotRecord(),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Not Record Rule");
    }
}
