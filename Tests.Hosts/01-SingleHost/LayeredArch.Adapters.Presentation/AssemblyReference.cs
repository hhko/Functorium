using System.Reflection;

namespace LayeredArch.Adapters.Presentation;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
