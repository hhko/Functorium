using ArchUnitNET.Loader;

namespace ECommerce.Tests.Unit.Architecture;

internal static class ArchitectureTestBase
{
    internal static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                ECommerce.Domain.AssemblyReference.Assembly,
                ECommerce.Application.AssemblyReference.Assembly)
            .Build();

    internal static readonly string DomainNamespace =
        typeof(ECommerce.Domain.AssemblyReference).Namespace!;
    internal static readonly string ApplicationNamespace =
        typeof(ECommerce.Application.AssemblyReference).Namespace!;
}
