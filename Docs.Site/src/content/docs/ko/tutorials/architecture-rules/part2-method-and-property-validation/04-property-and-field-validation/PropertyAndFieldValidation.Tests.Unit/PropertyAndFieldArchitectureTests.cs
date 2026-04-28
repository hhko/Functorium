using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace PropertyAndFieldValidation.Tests.Unit;

[Trait("Part2-Method-And-Property-Validation", "PropertyAndFieldValidation")]
public class PropertyAndFieldArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Product_ShouldHave_NameAndPriceProperties()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("PropertyAndFieldValidation.Domains")
            .And()
            .HaveNameEndingWith("Product")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireProperty("Name")
                .RequireProperty("Price"),
                verbose: true)
            .ThrowIfAnyFailures("Product Property Rule");
    }

    [Fact]
    public void OrderLine_ShouldHave_ProductNameAndQuantityProperties()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("PropertyAndFieldValidation.Domains")
            .And()
            .HaveNameEndingWith("OrderLine")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireProperty("ProductName")
                .RequireProperty("Quantity"),
                verbose: true)
            .ThrowIfAnyFailures("OrderLine Property Rule");
    }

    [Fact]
    public void DomainClasses_ShouldNotHave_PublicSetters()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("PropertyAndFieldValidation.Domains")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoPublicSetters(),
                verbose: true)
            .ThrowIfAnyFailures("No Public Setter Rule");
    }

    [Fact]
    public void DomainClasses_ShouldNotHave_InstanceFields()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("PropertyAndFieldValidation.Domains")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoInstanceFields(),
                verbose: true)
            .ThrowIfAnyFailures("No Instance Field Rule");
    }

    [Fact]
    public void DomainClasses_ShouldHave_OnlyPrimitiveProperties()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("PropertyAndFieldValidation.Domains")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireOnlyPrimitiveProperties(),
                verbose: true)
            .ThrowIfAnyFailures("Primitive Property Rule");
    }
}
