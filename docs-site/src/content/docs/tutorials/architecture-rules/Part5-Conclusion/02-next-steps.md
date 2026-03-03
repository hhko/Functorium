---
title: "5.2 다음 단계"
---

## 관련 튜토리얼

Functorium 프로젝트의 다른 튜토리얼을 통해 아키텍처 테스트와 관련된 패턴을 더 깊이 학습할 수 있습니다:

| 튜토리얼 | 관련 내용 |
|----------|----------|
| [함수형으로 성공 주도 값 객체 구현하기](../Implementing-Functional-ValueObject/) | ValueObject 패턴과 `Fin<T>` 반환 타입 |
| [CQRS Repository & Query 패턴 구현하기](../Implementing-CQRS-Repository-And-Query-Patterns/) | Entity, Repository, Command/Query 패턴 |
| [Specification 패턴 구현하기](../Implementing-Specification-Pattern/) | 도메인 레이어 Specification 패턴 |
| [타입 안전한 Usecase 파이프라인 설계하기](../Designing-TypeSafe-Usecase-Pipeline-Constraints/) | Application 레이어 Usecase 구조 |

## 프레임워크 확장

### 커스텀 IArchRule 구현

`DelegateArchRule`로는 표현하기 복잡한 규칙은 `IArchRule<T>`를 직접 구현합니다:

```csharp
public sealed class NoDuplicatePropertyNamesRule : IArchRule<Class>
{
    public string Description => "No duplicate property names across class hierarchy";

    public IReadOnlyList<RuleViolation> Validate(Class target, Architecture architecture)
    {
        var propertyNames = target.GetPropertyMembers()
            .Select(p => p.Name)
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (propertyNames.Count == 0)
            return [];

        return [new RuleViolation(
            target.FullName,
            nameof(NoDuplicatePropertyNamesRule),
            $"Duplicate properties: {string.Join(", ", propertyNames)}")];
    }
}
```

### 규칙 라이브러리 구축

팀에서 자주 사용하는 규칙 조합을 라이브러리로 패키징합니다:

```csharp
// 팀 공통 규칙 모음
public static class TeamArchRules
{
    public static readonly CompositeArchRule<Class> EntityRule = new(
        new ImmutabilityRule(),
        new NoDuplicatePropertyNamesRule(),
        FactoryMethodRule);

    private static readonly DelegateArchRule<Class> FactoryMethodRule = new(
        "Requires Create factory method",
        (target, _) => { /* ... */ });
}
```

## 참고 자료

### 공식 문서

| 자료 | 링크 |
|------|------|
| ArchUnitNET GitHub | https://github.com/TngTech/ArchUnitNET |
| ArchUnitNET Wiki | https://github.com/TngTech/ArchUnitNET/wiki |
| xUnit.net v3 | https://xunit.net/ |

### 관련 도서와 글

| 자료 | 설명 |
|------|------|
| *Clean Architecture* (Robert C. Martin) | 레이어 아키텍처의 원칙 |
| *Domain-Driven Design* (Eric Evans) | DDD 전술 패턴의 원전 |
| *Implementing Domain-Driven Design* (Vaughn Vernon) | DDD 패턴의 실전 구현 |

### Functorium 프로젝트

| 자료 | 경로 |
|------|------|
| ArchitectureRules 소스 코드 | `Src/Functorium.Testing/Assertions/ArchitectureRules/` |
| 실전 아키텍처 테스트 예제 (42개) | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture/` |
| API 레퍼런스 | [부록 A](../Appendix/A-api-reference.md) |

## 마무리

아키텍처 테스트는 팀의 설계 합의를 자동으로 검증하는 강력한 도구입니다. 이 튜토리얼에서 학습한 내용을 실제 프로젝트에 적용하여, 코드 리뷰의 부담을 줄이고 일관된 아키텍처를 유지하세요.
