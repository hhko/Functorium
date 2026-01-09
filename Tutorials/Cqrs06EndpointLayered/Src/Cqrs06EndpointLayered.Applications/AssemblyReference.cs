using System.Reflection;

namespace Cqrs06EndpointLayered.Applications;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
