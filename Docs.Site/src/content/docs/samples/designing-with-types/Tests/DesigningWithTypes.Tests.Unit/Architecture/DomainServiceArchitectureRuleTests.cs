using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Domains.Services;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace DesigningWithTypes.Tests.Unit.Architecture;

public sealed class DomainServiceArchitectureRuleTests : ArchitectureTestBase
{
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
    public void DomainService_ShouldBe_Stateless()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoInstanceFields("Repository"),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Stateless Rule");
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
