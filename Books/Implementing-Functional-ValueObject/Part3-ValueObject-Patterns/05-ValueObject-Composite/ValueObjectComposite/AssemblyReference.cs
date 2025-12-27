using System.Reflection;

namespace ValueObjectComposite;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
