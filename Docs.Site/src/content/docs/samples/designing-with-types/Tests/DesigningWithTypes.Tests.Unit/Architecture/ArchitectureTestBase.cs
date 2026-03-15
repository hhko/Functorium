using ArchUnitNET.Loader;

namespace DesigningWithTypes.Tests.Unit.Architecture;

public abstract class ArchitectureTestBase
{
    protected static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                DesigningWithTypes.AssemblyReference.Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(DesigningWithTypes.AssemblyReference).Namespace!;
}
