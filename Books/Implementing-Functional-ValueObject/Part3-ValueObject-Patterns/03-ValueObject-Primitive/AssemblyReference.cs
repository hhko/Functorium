using System.Reflection;

namespace ValueObjectPrimitive;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
