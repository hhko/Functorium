using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Unions;
using Functorium.Testing.Assertions.ArchitectureRules;
using LanguageExt;
using LanguageExt.Common;

namespace DesigningWithTypes.Tests.Unit.Architecture;

public sealed class ValueObjectArchitectureRuleTests : ArchitectureTestBase
{
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
            .And().AreNotAssignableTo(typeof(UnionValueObject))
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
            .And().AreNotAssignableTo(typeof(UnionValueObject))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Validate", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Validation<,>))),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Validate Method Rule");
    }
}
