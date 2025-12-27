using System.Reflection;

namespace TypeSafeEnum;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
