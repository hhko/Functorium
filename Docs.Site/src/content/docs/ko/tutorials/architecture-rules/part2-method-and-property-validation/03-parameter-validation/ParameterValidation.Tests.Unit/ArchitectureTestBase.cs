using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace ParameterValidation.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssembly(typeof(ParameterValidation.Domains.Address).Assembly)
        .Build();
}
