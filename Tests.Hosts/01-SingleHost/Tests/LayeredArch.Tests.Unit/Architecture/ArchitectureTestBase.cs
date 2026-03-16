using ArchUnitNET.Loader;

namespace LayeredArch.Tests.Unit.Architecture;

internal static class ArchitectureTestBase
{
    internal static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                typeof(Functorium.Adapters.Repositories.InMemoryRepositoryBase<,>).Assembly,
                LayeredArch.Domain.AssemblyReference.Assembly,
                LayeredArch.Application.AssemblyReference.Assembly,
                LayeredArch.Adapters.Persistence.AssemblyReference.Assembly,
                LayeredArch.Adapters.Presentation.AssemblyReference.Assembly,
                LayeredArch.Adapters.Infrastructure.AssemblyReference.Assembly)
            .Build();

    internal static readonly string DomainNamespace =
        typeof(LayeredArch.Domain.AssemblyReference).Namespace!;
    internal static readonly string ApplicationNamespace =
        typeof(LayeredArch.Application.AssemblyReference).Namespace!;
    internal static readonly string PersistenceNamespace =
        typeof(LayeredArch.Adapters.Persistence.AssemblyReference).Namespace!;
    internal static readonly string PresentationNamespace =
        typeof(LayeredArch.Adapters.Presentation.AssemblyReference).Namespace!;
    internal static readonly string InfrastructureNamespace =
        typeof(LayeredArch.Adapters.Infrastructure.AssemblyReference).Namespace!;
}
