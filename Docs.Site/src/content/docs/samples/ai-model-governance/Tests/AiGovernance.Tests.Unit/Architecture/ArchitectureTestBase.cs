using ArchUnitNET.Loader;

namespace AiGovernance.Tests.Unit.Architecture;

internal static class ArchitectureTestBase
{
    internal static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                typeof(AiGovernance.Domain.AggregateRoots.Models.AIModel).Assembly,
                AiGovernance.Application.AssemblyReference.Assembly)
            .Build();

    internal static readonly string DomainNamespace =
        typeof(AiGovernance.Domain.AggregateRoots.Models.AIModel).Namespace!
            .Replace(".AggregateRoots.Models", "");

    internal static readonly string ApplicationNamespace =
        typeof(AiGovernance.Application.AssemblyReference).Namespace!;
}
