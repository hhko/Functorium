using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace PropertyAndFieldValidation.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssembly(typeof(PropertyAndFieldValidation.Domains.Product).Assembly)
        .Build();
}
