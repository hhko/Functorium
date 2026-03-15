using ArchUnitNET.Loader;

namespace ECommerce.Tests.Unit.Architecture;

public abstract class ArchitectureTestBase
{
    protected static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                ECommerce.Domain.AssemblyReference.Assembly,
                ECommerce.Application.AssemblyReference.Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(ECommerce.Domain.AssemblyReference).Namespace!;
    protected static readonly string ApplicationNamespace =
        typeof(ECommerce.Application.AssemblyReference).Namespace!;
}
