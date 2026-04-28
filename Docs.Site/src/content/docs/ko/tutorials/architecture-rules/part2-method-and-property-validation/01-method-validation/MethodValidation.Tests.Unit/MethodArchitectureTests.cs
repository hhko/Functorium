using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace MethodValidation.Tests.Unit;

[Trait("Part2-Method-And-Property-Validation", "MethodValidation")]
public class MethodArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void FactoryMethods_ShouldBe_PublicAndStatic()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("MethodValidation.Domains")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("Create", m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()),
                verbose: true)
            .ThrowIfAnyFailures("Factory Method Rule");
    }

    [Fact]
    public void InstanceMethods_ShouldNotBe_Static()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("MethodValidation.Domains")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethodIfExists("Add", m => m
                    .RequireNotStatic()),
                verbose: true)
            .ThrowIfAnyFailures("Instance Method Rule");
    }

    [Fact]
    public void ExtensionMethods_ShouldBe_ExtensionMethods()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ResideInNamespace("MethodValidation.Extensions")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllMethods(
                    m => !m.Name.StartsWith(".") && m.MethodForm == MethodForm.Normal,
                    m => m
                        .RequireStatic()
                        .RequireExtensionMethod()),
                verbose: true)
            .ThrowIfAnyFailures("Extension Method Rule");
    }
}
