namespace Functorium.Applications.Observabilities;

/// <summary>
/// 이 속성이 적용된 인터페이스, 프로퍼티 또는 record 생성자 파라미터의 값은
/// CtxEnricher에서 ctx 루트 레벨(ctx.{field})로 출력됩니다.
/// 인터페이스에 적용하면 해당 인터페이스를 구현하는 모든 Request/Response/Event에서 승격됩니다.
/// Pillar 타겟팅([CtxTarget])과 독립적으로 동작합니다 (네이밍에만 영향).
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CtxRootAttribute : Attribute;
