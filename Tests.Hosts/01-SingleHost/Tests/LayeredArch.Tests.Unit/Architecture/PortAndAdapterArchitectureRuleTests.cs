using ArchUnitNET.Fluent;
using Functorium.Domains.Observabilities;
using Functorium.Domains.Services;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class PortAndAdapterArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void AdapterImplementation_ShouldHave_GeneratePipelineAttribute()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IPort))
            .And().AreNotAbstract()
            .And().DoNotHaveNameEndingWith("Pipeline")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAttribute("GeneratePipeline"),
                verbose: true)
            .ThrowIfAnyFailures("Adapter GeneratePipeline Attribute Rule");
    }

    [Fact]
    public void AdapterImplementation_ShouldHave_RequestCategoryProperty()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IPort))
            .And().AreNotAbstract()
            .And().DoNotHaveNameEndingWith("Pipeline")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod("get_RequestCategory", m => m
                    .RequireVisibility(ArchUnitNET.Domain.Visibility.Public)),
                verbose: true)
            .ThrowIfAnyFailures("Adapter RequestCategory Property Rule");
    }

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
}
