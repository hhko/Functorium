using System.Reflection;

namespace Cqrs05EndpointLayered.Adapters.Infrastructure;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
