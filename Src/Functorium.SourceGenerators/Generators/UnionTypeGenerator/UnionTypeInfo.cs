using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.UnionTypeGenerator;

/// <summary>
/// Union 타입 생성에 필요한 메타데이터.
/// </summary>
public readonly record struct UnionTypeInfo
{
    /// <summary>
    /// Union 타입의 네임스페이스.
    /// </summary>
    public readonly string Namespace;

    /// <summary>
    /// Union 타입 이름.
    /// </summary>
    public readonly string TypeName;

    /// <summary>
    /// sealed record 케이스 이름 목록.
    /// </summary>
    public readonly string[] CaseNames;

    /// <summary>
    /// 원본 소스 위치 (진단용).
    /// </summary>
    public readonly Location? Location;

    /// <summary>
    /// 빈 값을 나타내는 상수.
    /// </summary>
    public static readonly UnionTypeInfo None = new(string.Empty, string.Empty, [], null);

    public UnionTypeInfo(
        string @namespace,
        string typeName,
        string[] caseNames,
        Location? location)
    {
        Namespace = @namespace;
        TypeName = typeName;
        CaseNames = caseNames;
        Location = location;
    }
}
