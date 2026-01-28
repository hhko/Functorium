using Microsoft.CodeAnalysis;

namespace Functorium.Adapters.SourceGenerator.Generators.EntityIdGenerator;

/// <summary>
/// Entity ID 생성에 필요한 메타데이터.
/// </summary>
public readonly record struct EntityIdInfo
{
    /// <summary>
    /// Entity 클래스의 네임스페이스.
    /// </summary>
    public readonly string Namespace;

    /// <summary>
    /// Entity 클래스 이름.
    /// </summary>
    public readonly string EntityName;

    /// <summary>
    /// 생성될 ID 타입 이름 (기본값: {EntityName}Id).
    /// </summary>
    public readonly string IdTypeName;

    /// <summary>
    /// 원본 소스 위치 (진단용).
    /// </summary>
    public readonly Location? Location;

    /// <summary>
    /// 빈 값을 나타내는 상수.
    /// </summary>
    public static readonly EntityIdInfo None = new(string.Empty, string.Empty, string.Empty, null);

    public EntityIdInfo(
        string @namespace,
        string entityName,
        string idTypeName,
        Location? location)
    {
        Namespace = @namespace;
        EntityName = entityName;
        IdTypeName = idTypeName;
        Location = location;
    }
}
