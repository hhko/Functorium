using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 아키텍처 규칙을 나타내는 인터페이스입니다.
/// 각 규칙을 독립 객체로 표현하여 OCP를 달성합니다.
/// </summary>
/// <typeparam name="TType">검증 대상 타입 (Class 또는 Interface)</typeparam>
public interface IArchRule<in TType> where TType : IType
{
    /// <summary>
    /// 규칙에 대한 설명을 반환합니다.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 주어진 대상 타입에 대해 규칙을 검증합니다.
    /// </summary>
    /// <param name="target">검증 대상 타입</param>
    /// <param name="architecture">아키텍처 정보</param>
    /// <returns>위반 목록 (위반이 없으면 빈 리스트)</returns>
    IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture);
}
