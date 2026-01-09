using System.Reflection;

namespace Cqrs06EndpointLayered.Adapters.Presentation;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
