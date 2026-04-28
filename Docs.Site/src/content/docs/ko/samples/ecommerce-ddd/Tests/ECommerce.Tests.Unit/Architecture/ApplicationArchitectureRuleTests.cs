using Functorium.Testing.Assertions.ArchitectureRules.Suites;

namespace ECommerce.Tests.Unit.Architecture;

public sealed class ApplicationArchitectureRuleTests : ApplicationArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string ApplicationNamespace => ArchitectureTestBase.ApplicationNamespace;
}
