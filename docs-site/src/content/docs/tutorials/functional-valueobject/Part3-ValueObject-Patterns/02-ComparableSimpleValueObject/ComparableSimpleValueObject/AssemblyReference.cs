using System.Reflection;

namespace ComparableSimpleValueObject;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
