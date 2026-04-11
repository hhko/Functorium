---
title: "Next Steps"
---

## Overview

이 튜토리얼에서는 ArchUnitNET과 Functorium의 Validator 확장을 활용하여 아키텍처 규칙을 코드로 표현하고, 자동으로 검증하는 방법을 학습했습니다. 하지만 이것은 시작점입니다. 아키텍처 테스트는 도메인 모델링, CQRS, Specification 패턴 등 다른 설계 패턴과 결합될 때 진정한 가치를 발휘합니다.

> **"아키텍처 테스트는 팀의 설계 합의를 코드에 새기는 것입니다. 합의가 성장하면 테스트도 함께 성장합니다."**

## 관련 튜토리얼

Functorium 프로젝트의 다른 튜토리얼을 통해 아키텍처 테스트와 관련된 패턴을 더 깊이 학습할 수 있습니다:

| 튜토리얼 | 관련 내용 |
|----------|----------|
| [함수형으로 성공 주도 value object 구현하기](../functional-valueobject/) | ValueObject 패턴과 `Fin<T>` 반환 타입 |
| [CQRS Repository & Query 패턴 구현하기](../cqrs-repository/) | Entity, Repository, Command/Query 패턴 |
| [Specification 패턴 구현하기](../specification-pattern/) | 도메인 레이어 Specification 패턴 |
| [타입 안전한 Usecase 파이프라인 설계하기](../usecase-pipeline/) | Application 레이어 Usecase 구조 |

## 프레임워크 확장

### 커스텀 IArchRule 구현

`DelegateArchRule`로는 표현하기 복잡한 규칙은 `IArchRule<T>`를 직접 implements:

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

팀에서 자주 사용하는 규칙 조합을 라이브러리로 패키징하면, 새 프로젝트에서도 동일한 아키텍처 기준을 즉시 적용할 수 있습니다:

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

## References

### Official Documentation

| 자료 | 링크 |
|------|------|
| ArchUnitNET GitHub | https://github.com/TngTech/ArchUnitNET |
| ArchUnitNET Wiki | https://github.com/TngTech/ArchUnitNET/wiki |
| xUnit.net v3 | https://xunit.net/ |

### 관련 도서와 글

| 자료 | Description |
|------|------|
| *Clean Architecture* (Robert C. Martin) | 레이어 아키텍처의 원칙 |
| *Domain-Driven Design* (Eric Evans) | DDD 전술 패턴의 원전 |
| *Implementing Domain-Driven Design* (Vaughn Vernon) | DDD 패턴의 실전 구현 |

### Functorium 프로젝트

| 자료 | 경로 |
|------|------|
| ArchitectureRules 소스 코드 | `Src/Functorium.Testing/Assertions/ArchitectureRules/` |
| 사전 구축 테스트 스위트 | [Part 4-05 아키텍처 테스트 스위트](../Part4-Real-World-Patterns/05-Architecture-Test-Suites/) |
| 실전 아키텍처 테스트 예제 (42개) | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture/` |
| API 레퍼런스 | [부록 A](../Appendix/A-api-reference.md) |

더 자세한 API 사용법은 [부록 A: API 레퍼런스](../Appendix/A-api-reference.md)에서, ArchUnitNET의 핵심 패턴은 [부록 B: ArchUnitNET 치트시트](../Appendix/B-archunitnet-cheatsheet.md)에서, 자주 묻는 질문은 [부록 C: FAQ](../Appendix/C-faq.md)에서 확인할 수 있습니다.

## FAQ

### Q1: 아키텍처 테스트를 처음 도입할 때 어떤 규칙부터 시작해야 하나요?
**A**: 레이어 의존성 규칙 하나부터 시작하세요. 예를 들어 "Domain 레이어는 Infrastructure에 의존하지 않는다"처럼 가장 기본적인 규칙을 추가하면, 코드 리뷰 부담을 줄이면서 아키텍처 위반을 자동으로 감지할 수 있습니다.

### Q2: `DelegateArchRule`과 `IArchRule<T>` 직접 구현은 언제 구분하나요?
**A**: 단순한 조건 검사는 `DelegateArchRule`로 충분합니다. 클래스 계층 탐색, 속성 그룹핑 등 복잡한 로직이 필요하면 `IArchRule<T>`를 직접 구현하는 것이 가독성과 재사용성 면에서 유리합니다.

### Q3: 아키텍처 테스트를 CI에 통합할 때 주의할 점은 무엇인가요?
**A**: 아키텍처 테스트는 일반 단위 테스트와 동일하게 `dotnet test`로 실행되므로 CI 파이프라인에 별도 설정 없이 통합할 수 있습니다. 다만 규칙이 많아지면 실행 시간이 늘어날 수 있으므로, 규칙을 논리적 그룹으로 나누어 병렬 실행하는 것을 권장합니다.

### Q4: 다른 Functorium 튜토리얼과 아키텍처 테스트는 어떻게 연결되나요?
**A**: CQRS 튜토리얼에서 배운 `IRepository`, `IQueryPort` 등의 레이어 경계를 아키텍처 테스트로 강제할 수 있고, Specification 패턴의 도메인 계층 위치도 검증할 수 있습니다. 아키텍처 테스트는 다른 패턴들이 의도한 위치에서 사용되고 있는지 자동으로 확인하는 안전장치입니다.

---

## Wrapping Up

이 튜토리얼에서 배운 패턴은 시작점입니다. 아키텍처 테스트의 진정한 힘은 팀이 합의한 설계 원칙을 코드로 표현하고, CI가 매 커밋마다 자동으로 검증하는 데 있습니다. 작은 규칙 하나부터 시작하세요. 레이어 의존성 규칙 하나만으로도 코드 리뷰의 부담을 줄이고, 아키텍처가 의도대로 유지되고 있다는 확신을 팀에 줄 수 있습니다.

---

부록에서는 전체 API 레퍼런스, ArchUnitNET 치트시트, 자주 묻는 질문을 정리합니다.

→ [부록 A: API 레퍼런스](../Appendix/A-api-reference.md)
