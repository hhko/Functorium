namespace ObservabilityHost.Usecases;

/// <summary>
/// 운영자(Operator) 컨텍스트 인터페이스.
/// [LogEnricherRoot] 없이 비-root 인터페이스로 선언되어
/// ctx.operator_context.{field} 형식의 인터페이스 스코프 필드를 생성합니다.
/// </summary>
public interface IOperatorContext { string OperatorId { get; } }
