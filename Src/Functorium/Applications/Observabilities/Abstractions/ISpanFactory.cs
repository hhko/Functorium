namespace Functorium.Applications.Observabilities;

/// <summary>
/// Span을 생성하는 팩토리 인터페이스입니다.
/// Abstract Factory 패턴을 사용하여 기술 독립적인 Span 생성을 제공합니다.
/// </summary>
public interface ISpanFactory
{
    /// <summary>
    /// 새로운 루트 또는 자식 Span을 생성합니다.
    /// 현재 컨텍스트의 Span이 있으면 자동으로 부모로 연결됩니다.
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <param name="category">카테고리 (예: "Usecase", "Repository")</param>
    /// <param name="handler">핸들러 이름</param>
    /// <param name="method">메서드 이름</param>
    /// <returns>새로운 Span 또는 추적이 비활성화된 경우 null</returns>
    ISpan? CreateSpan(
        string operationName,
        string category,
        string handler,
        string method);

    /// <summary>
    /// 지정된 부모 컨텍스트 아래에 자식 Span을 생성합니다.
    /// </summary>
    /// <param name="parentContext">부모 컨텍스트 (null인 경우 현재 컨텍스트 사용)</param>
    /// <param name="operationName">작업 이름</param>
    /// <param name="category">카테고리</param>
    /// <param name="handler">핸들러 이름</param>
    /// <param name="method">메서드 이름</param>
    /// <returns>새로운 자식 Span 또는 추적이 비활성화된 경우 null</returns>
    ISpan? CreateChildSpan(
        IObservabilityContext? parentContext,
        string operationName,
        string category,
        string handler,
        string method);
}
