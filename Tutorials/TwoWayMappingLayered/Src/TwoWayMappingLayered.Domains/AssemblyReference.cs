using System.Reflection;

namespace TwoWayMappingLayered.Domains;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
