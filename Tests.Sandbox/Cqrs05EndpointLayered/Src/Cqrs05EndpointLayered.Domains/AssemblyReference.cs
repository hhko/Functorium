using System.Reflection;

namespace Cqrs05EndpointLayered.Domains;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
