using System.Reflection;

namespace Cqrs05EndpointLayered.Applications;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
