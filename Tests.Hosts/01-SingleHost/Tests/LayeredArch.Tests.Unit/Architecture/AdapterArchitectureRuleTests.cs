using ArchUnitNET.Fluent;
using Functorium.Domains.Observabilities;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class AdapterArchitectureRuleTests
{
    [Fact]
    public void Adapter_ShouldHave_VirtualMethods()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IObservablePort))
            .And().AreNotAbstract()
            .And().DoNotHaveNameEndingWith("Observable")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireAllMethods(method => method
                    .RequireVirtual()),
                verbose: true)
            .ThrowIfAnyFailures("Adapter Virtual Methods Rule");
    }

    [Fact]
    public void Adapter_ShouldHave_RequestCategoryProperty()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IObservablePort))
            .And().AreNotAbstract()
            .And().DoNotHaveNameEndingWith("Observable")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireProperty("RequestCategory"),
                verbose: true)
            .ThrowIfAnyFailures("Adapter RequestCategory Property Rule");
    }

    [Fact]
    public void Adapter_ShouldHave_GenerateObservablePortAttribute()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IObservablePort))
            .And().AreNotAbstract()
            .And().DoNotHaveNameEndingWith("Observable")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireAttribute("GenerateObservablePort"),
                verbose: true)
            .ThrowIfAnyFailures("Adapter GenerateObservablePort Attribute Rule");
    }

    [Fact]
    public void Adapter_ShouldNotBe_Sealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IObservablePort))
            .And().AreNotAbstract()
            .And().DoNotHaveNameEndingWith("Observable")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireNotSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Adapter Not Sealed Rule");
    }
}
