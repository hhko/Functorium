using Functorium.Testing.Assertions.ArchitectureRules.Suites;

namespace ECommerce.Tests.Unit.Architecture;

public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string DomainNamespace => ArchitectureTestBase.DomainNamespace;
}
