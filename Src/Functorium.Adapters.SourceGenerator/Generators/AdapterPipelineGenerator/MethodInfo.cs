namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

public class MethodInfo
{
    public string Name { get; }
    public List<ParameterInfo> Parameters { get; }
    public string ReturnType { get; }

    public MethodInfo(string name, List<ParameterInfo> parameters, string returnType)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
    }
}
