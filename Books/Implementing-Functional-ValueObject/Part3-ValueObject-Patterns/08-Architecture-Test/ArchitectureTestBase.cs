using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using SimpleValueObject;
using ComparableSimpleValueObject;
using ValueObjectPrimitive;
using ComparableValueObjectPrimitive;
using ValueObjectComposite;
using ComparableValueObjectComposite;
using TypeSafeEnum;

namespace ArchitectureTest;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = BuildArchitecture();

    private static Architecture BuildArchitecture()
    {
        List<System.Reflection.Assembly> assemblies = [];

        assemblies.AddRange([
            SimpleValueObject.AssemblyReference.Assembly,
            ComparableSimpleValueObject.AssemblyReference.Assembly,
            ValueObjectPrimitive.AssemblyReference.Assembly,
            ComparableValueObjectPrimitive.AssemblyReference.Assembly,
            ValueObjectComposite.AssemblyReference.Assembly,
            ComparableValueObjectComposite.AssemblyReference.Assembly,
            TypeSafeEnum.AssemblyReference.Assembly
        ]);

        return new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
    }
}
