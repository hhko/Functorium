namespace SpecificationPattern.Demo.Domain;

/// <summary>
/// Specification 학습용 도메인 모델.
/// </summary>
public record Product(string Name, decimal Price, int Stock, string Category);
