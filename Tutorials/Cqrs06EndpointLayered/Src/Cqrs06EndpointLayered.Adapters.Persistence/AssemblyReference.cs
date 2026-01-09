using System.Reflection;

namespace Cqrs06EndpointLayered.Adapters.Persistence;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
