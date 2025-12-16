namespace SourceGenerator.Demo.Adapters;

/// <summary>
/// Adapter 마커 인터페이스.
/// Source Generator는 이 인터페이스를 구현하는 클래스에서
/// [GeneratePipeline] 어트리뷰트가 있을 때 Pipeline 래퍼를 생성합니다.
/// </summary>
public interface IAdapter
{
    /// <summary>
    /// Adapter의 카테고리 (예: "repository", "api", "cache")
    /// Observability 로깅에서 분류 용도로 사용됩니다.
    /// </summary>
    string RequestCategory { get; }
}
