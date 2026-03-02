namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 아키텍처 규칙 위반을 나타내는 구조적 레코드입니다.
/// </summary>
/// <param name="TargetName">위반이 발생한 대상 타입의 이름</param>
/// <param name="RuleName">위반된 규칙의 이름</param>
/// <param name="Description">위반에 대한 상세 설명</param>
public sealed record RuleViolation(string TargetName, string RuleName, string Description);
