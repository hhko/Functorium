using System.Reflection;

namespace LayeredArch.Adapters.Persistence;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
