using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace ParameterValidation.Tests.Unit;

[Trait("Part2-Method-And-Property-Validation", "ParameterValidation")]
public class ParameterArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void AddressCreate_ShouldHave_ThreeParameters()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ParameterValidation.Domains")
            .And()
            .HaveNameEndingWith("Address")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireParameterCount(3)),
                verbose: true)
            .ThrowIfAnyFailures("Address Parameter Count Rule");
    }

    [Fact]
    public void CoordinateCreate_ShouldHave_TwoParameters()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ParameterValidation.Domains")
            .And()
            .HaveNameEndingWith("Coordinate")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireParameterCount(2)),
                verbose: true)
            .ThrowIfAnyFailures("Coordinate Parameter Count Rule");
    }

    [Fact]
    public void FactoryMethods_ShouldHave_AtLeastOneParameter()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ParameterValidation.Domains")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireParameterCountAtLeast(1)),
                verbose: true)
            .ThrowIfAnyFailures("Factory Method Minimum Parameter Rule");
    }

    [Fact]
    public void AddressCreate_ShouldHave_StringFirstParameter()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ParameterValidation.Domains")
            .And()
            .HaveNameEndingWith("Address")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireFirstParameterTypeContaining("String")),
                verbose: true)
            .ThrowIfAnyFailures("Address First Parameter Type Rule");
    }

    [Fact]
    public void CoordinateCreate_ShouldHave_DoubleParameter()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("ParameterValidation.Domains")
            .And()
            .HaveNameEndingWith("Coordinate")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireAnyParameterTypeContaining("Double")),
                verbose: true)
            .ThrowIfAnyFailures("Coordinate Double Parameter Rule");
    }
}
