using System.Reflection;

namespace Cqrs05EndpointLayered.Adapters.Persistence;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
