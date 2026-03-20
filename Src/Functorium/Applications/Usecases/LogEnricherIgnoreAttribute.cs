namespace Functorium.Applications.Usecases;

/// <summary>
/// 이 속성이 적용된 Request record, 프로퍼티 또는 record 생성자 파라미터는
/// LogEnricher 자동 생성에서 제외됩니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class LogEnricherIgnoreAttribute : Attribute;
