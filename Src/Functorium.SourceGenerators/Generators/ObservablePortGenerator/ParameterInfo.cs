using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public RefKind RefKind { get; }
    public bool IsCollection { get; }
    public bool IsComplexType { get; }
    public bool NeedsToString { get; }

    /// <summary>
    /// 생성자 파라미터용 (타입 분석 없음)
    /// </summary>
    public ParameterInfo(string name, string type, RefKind refKind)
    {
        Name = name;
        Type = type;
        RefKind = refKind;
        IsCollection = CollectionTypeHelper.IsCollectionType(type);
    }

    /// <summary>
    /// 메서드 파라미터용 (타입 분석 포함)
    /// </summary>
    public ParameterInfo(string name, string type, RefKind refKind, bool isComplexType, bool needsToString)
    {
        Name = name;
        Type = type;
        RefKind = refKind;
        IsCollection = CollectionTypeHelper.IsCollectionType(type);
        IsComplexType = isComplexType;
        NeedsToString = needsToString;
    }
}
