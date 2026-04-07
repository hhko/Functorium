namespace Functorium.Abstractions.Observabilities;

/// <summary>
/// 이 속성이 적용된 Request record, 프로퍼티 또는 record 생성자 파라미터는
/// CtxEnricher 자동 생성에서 모든 Pillar(Logging, Tracing, Metrics)에서 제외됩니다.
/// [CtxTarget]보다 우선합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CtxIgnoreAttribute : Attribute;
