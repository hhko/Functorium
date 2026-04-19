namespace Functorium.Adapters.SourceGenerators;

/// <summary>
/// EF Core 모델 클래스에 이 속성을 적용하면 ExecuteUpdate용 ApplySetters 메서드가 자동으로 생성됩니다.
/// Id 프로퍼티는 자동 제외되며, [SetterIgnore]로 개별 프로퍼티를 제외할 수 있습니다.
/// </summary>
/// <remarks>
/// 이 Attribute를 사용하려면 프로젝트에서 Functorium.SourceGenerators를 참조해야 합니다.
/// 대상 클래스는 partial로 선언해야 합니다.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateSettersAttribute : Attribute;
