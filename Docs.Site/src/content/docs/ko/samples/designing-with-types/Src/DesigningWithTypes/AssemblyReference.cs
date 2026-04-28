using System.Reflection;

namespace DesigningWithTypes;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
