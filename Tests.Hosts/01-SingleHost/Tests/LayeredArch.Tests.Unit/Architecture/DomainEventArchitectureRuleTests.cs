using ArchUnitNET.Fluent;
using Functorium.Domains.Events;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class DomainEventArchitectureRuleTests : ArchitectureTestBase
{
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
}
