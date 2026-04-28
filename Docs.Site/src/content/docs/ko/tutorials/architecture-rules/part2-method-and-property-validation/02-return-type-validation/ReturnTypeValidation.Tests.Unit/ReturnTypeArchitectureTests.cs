using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;
using LanguageExt;
using Xunit;

namespace ReturnTypeValidation.Tests.Unit;

[Trait("Part2-Method-And-Property-Validation", "ReturnTypeValidation")]
public class ReturnTypeArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void CreateMethods_ShouldReturn_FinOpenGeneric()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ReturnTypeValidation.Domains")
            .And()
            .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireReturnType(typeof(Fin<>))),
                verbose: true)
            .ThrowIfAnyFailures("Fin Return Type Rule");
    }

    [Fact]
    public void CreateFromValidated_ShouldReturn_DeclaringClass()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ReturnTypeValidation.Domains")
            .And()
            .HaveNameEndingWith("Customer")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("CreateFromValidated", m => m
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("Factory Return Type Rule");
    }

    [Fact]
    public void CreateMethods_ShouldReturn_TypeContainingFin()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ReturnTypeValidation.Domains")
            .And()
            .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireReturnTypeContaining("Fin")),
                verbose: true)
            .ThrowIfAnyFailures("Fin Return Type Containing Rule");
    }
}
