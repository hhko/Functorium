using Microsoft.CodeAnalysis;

namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

public readonly record struct PipelineClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;

    public static readonly PipelineClassInfo None = new(string.Empty, string.Empty, new List<MethodInfo>(), new List<ParameterInfo>(), null);

    public PipelineClassInfo(
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
