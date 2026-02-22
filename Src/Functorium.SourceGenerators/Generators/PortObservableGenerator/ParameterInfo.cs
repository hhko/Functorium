using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.PortObservableGenerator;

public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public RefKind RefKind { get; }
    public bool IsCollection { get; }

    public ParameterInfo(string name, string type, RefKind refKind)
    {
        Name = name;
        Type = type;
        RefKind = refKind;
        IsCollection = CollectionTypeHelper.IsCollectionType(type);
    }
}
