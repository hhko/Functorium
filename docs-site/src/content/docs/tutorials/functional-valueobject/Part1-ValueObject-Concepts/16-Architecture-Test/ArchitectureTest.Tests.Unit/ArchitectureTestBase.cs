using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace ArchitectureTest.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = BuildArchitecture();

    private static Architecture BuildArchitecture()
    {
        List<System.Reflection.Assembly> assemblies = [];

        assemblies.AddRange([
            ArchitectureTest.AssemblyReference.Assembly,
        ]);

        return new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
    }
}
