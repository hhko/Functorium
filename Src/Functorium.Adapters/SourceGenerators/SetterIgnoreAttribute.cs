namespace Functorium.Adapters.SourceGenerators;

/// <summary>
/// [GenerateSetters] 대상 클래스에서 특정 프로퍼티를 ExecuteUpdate SetProperty 대상에서 제외합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SetterIgnoreAttribute : Attribute;
