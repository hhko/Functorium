using ArchUnitNET.Loader;
using Functorium.Domains.ValueObjects.Unions;
using Functorium.Testing.Assertions.ArchitectureRules.Suites;

namespace DesigningWithTypes.Tests.Unit.Architecture;

public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    private static readonly ArchUnitNET.Domain.Architecture s_architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
            DesigningWithTypes.AssemblyReference.Assembly)
        .Build();

    protected override ArchUnitNET.Domain.Architecture Architecture => s_architecture;
    protected override string DomainNamespace =>
        typeof(DesigningWithTypes.AssemblyReference).Namespace!;
    protected override IReadOnlyList<System.Type> ValueObjectExcludeFromFactoryMethods =>
        [typeof(UnionValueObject)];
    protected override string[] DomainServiceAllowedFieldTypes => ["Repository"];
}
