namespace Functorium.Adapters.SourceGenerators;

/// <summary>
/// Adapter 클래스에 이 속성을 적용하면 Pipeline wrapper 클래스가 자동으로 생성됩니다.
/// 생성되는 Pipeline은 OpenTelemetry 기반의 Observability(Tracing, Logging, Metrics)를 제공합니다.
/// </summary>
/// <remarks>
/// 이 Attribute를 사용하려면 프로젝트에서 Functorium.SourceGenerators를 참조해야 합니다.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePipelineAttribute : Attribute;
