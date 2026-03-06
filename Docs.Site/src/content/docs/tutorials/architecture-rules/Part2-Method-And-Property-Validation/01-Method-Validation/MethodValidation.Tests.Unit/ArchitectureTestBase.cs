using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace MethodValidation.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssembly(typeof(MethodValidation.Domains.Money).Assembly)
        .Build();
}
