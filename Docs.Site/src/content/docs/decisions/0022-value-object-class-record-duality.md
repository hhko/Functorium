---
title: "ADR-0022: Value Object의 Class/Record 이중 계층"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

DDD에서 Value Object는 불변성과 값 등치를 보장해야 합니다. 그러나 전통적인 Value Object(이메일, 금액 등)와 Discriminated Union 형태의 Value Object(주문 상태, 결제 방식 등)는 요구사항이 다릅니다.

- **전통적 VO**: 단일 값 또는 복합 값을 감싸며, 등치 비교와 유효성 검증이 핵심입니다.
- **Discriminated Union VO**: 유한한 상태 집합을 표현하며, 상태 전이와 패턴 매칭이 핵심입니다.

하나의 계층으로 두 가지 요구사항을 모두 만족시키려면 과도한 복잡도가 발생하거나 한쪽의 표현력이 희생됩니다.

## 검토한 옵션

1. Class 계층 + Record 계층 병행
2. 단일 Class 계층
3. 단일 Record 계층
4. Interface only (구현 없는 인터페이스)

## 결정

**선택한 옵션: "Class 계층 + Record 계층 병행"**, 전통적 VO와 Discriminated Union VO의 요구사항이 근본적으로 다르므로, 각각에 최적화된 계층을 제공하기 때문입니다.

**Class 계층** (전통적 Value Object):
- `AbstractValueObject` → `ValueObject` → `SimpleValueObject<T>` / `ComparableSimpleValueObject<T>`
- 등치 비교(`Equals`, `GetHashCode`)가 기반 클래스에서 처리됩니다.
- `SimpleValueObject<T>`는 단일 값 래핑, `ComparableSimpleValueObject<T>`는 비교 가능한 값 래핑에 사용됩니다.

**Record 계층** (Discriminated Union):
- `UnionValueObject<TSelf>`
- C# record의 구조적 등치와 `with` 표현식을 활용합니다.
- sealed record 상속으로 유한한 상태 집합을 표현합니다.

### 결과

- Good, because 전통적 VO와 Union VO 각각에 최적화된 기반 타입을 제공합니다.
- Good, because Class 계층에서 등치 비교 보일러플레이트가 제거됩니다.
- Good, because Record 계층에서 C#의 패턴 매칭과 `switch` 표현식을 자연스럽게 활용할 수 있습니다.
- Bad, because 두 가지 계층이 공존하여 어떤 기반 타입을 선택해야 하는지 판단이 필요합니다.

### 확인

- Value Object가 두 계층 중 하나를 반드시 상속하는지 아키텍처 규칙 테스트로 확인합니다.
- Union Value Object의 sealed record 계층이 완전한 상태 집합을 표현하는지 코드 리뷰에서 점검합니다.

## 옵션별 장단점

### Class 계층 + Record 계층 병행

- Good, because 전통적 VO는 Class 계층에서, Union VO는 Record 계층에서 최적의 표현력을 가집니다.
- Good, because 각 계층이 해당 패턴에 필요한 기능만 제공하여 단순합니다.
- Good, because C# record의 구조적 등치와 패턴 매칭을 Union VO에서 직접 활용합니다.
- Bad, because 두 가지 기반 타입 중 선택이 필요하여 가이드라인 문서가 필요합니다.

### 단일 Class 계층

- Good, because 모든 VO가 동일한 기반 타입을 사용하여 일관됩니다.
- Bad, because Discriminated Union 패턴을 Class 계층으로 표현하면 패턴 매칭이 부자연스럽습니다.
- Bad, because 상태 전이 표현에 필요한 sealed 계층 구조를 Class로 구현하면 복잡합니다.

### 단일 Record 계층

- Good, because C# record의 구조적 등치를 모든 VO에서 활용합니다.
- Bad, because 복합 값 타입의 등치 비교 커스터마이징이 record에서는 어렵습니다.
- Bad, because record의 `with` 표현식이 불변성을 우회할 수 있는 여지를 남깁니다.

### Interface only (구현 없는 인터페이스)

- Good, because 구현 선택이 완전히 자유롭습니다.
- Bad, because 등치 비교, 유효성 검증 등 공통 로직이 모든 구현체에서 중복됩니다.
- Bad, because 일관된 VO 동작을 보장할 수 없습니다.

## 관련 정보

- 관련 커밋: `5c347e54`
- 관련 스펙: `spec/02-value-object`
- 관련 튜토리얼: `Docs.Site/src/content/docs/tutorials/functional-valueobject/`
