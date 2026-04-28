using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace ReturnTypeValidation.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssembly(typeof(ReturnTypeValidation.Domains.Email).Assembly)
        .Build();
}
