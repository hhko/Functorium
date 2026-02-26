namespace Functorium.Domains.Entities;

/// <summary>
/// Entity 클래스에 이 속성을 적용하면 해당 Entity의 ID 타입이 자동으로 생성됩니다.
/// 생성되는 타입: {EntityName}Id, {EntityName}IdComparer, {EntityName}IdConverter
/// </summary>
/// <remarks>
/// 이 Attribute를 사용하려면 프로젝트에서 Functorium.SourceGenerators를 참조해야 합니다.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEntityIdAttribute : Attribute;
