using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureRules.Tests.Unit;

[Trait("Part4-ArchitectureRules", "Naming")]
public class SpecificationNamingTests
{
    private static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssembly(typeof(Domain.AggregateRoots.Products.Product).Assembly)
            .Build();

    [Fact]
    public void Specifications_ShouldEndWithSpec()
    {
        var rule = Classes()
            .That()
            .ResideInNamespaceMatching(@".*\.Specifications")
            .Should()
            .HaveNameEndingWith("Spec");

        rule.Check(Architecture);
    }

    [Fact]
    public void Specifications_ShouldResideInSpecificationsNamespace()
    {
        var rule = Classes()
            .That()
            .HaveNameEndingWith("Spec")
            .Should()
            .ResideInNamespaceMatching(@".*\.Specifications");

        rule.Check(Architecture);
    }
}
