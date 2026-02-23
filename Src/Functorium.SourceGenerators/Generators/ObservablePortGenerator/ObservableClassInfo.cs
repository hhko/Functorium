using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

public readonly record struct ObservableClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;

    public static readonly ObservableClassInfo None = new(string.Empty, string.Empty, new List<MethodInfo>(), new List<ParameterInfo>(), null);

    public ObservableClassInfo(
        string @namespace,
        string className,
        List<MethodInfo> methods,
        List<ParameterInfo> baseConstructorParameters,
        Location? location)
    {
        Namespace = @namespace;
        ClassName = className;
        Methods = methods;
        BaseConstructorParameters = baseConstructorParameters;
        Location = location;
    }
}
