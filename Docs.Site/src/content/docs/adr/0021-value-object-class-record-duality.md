---
title: "ADR-0021: Value Object의 Class/Record 이중 계층"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

`Email` Value Object를 구현한다고 가정합니다. 핵심은 문자열 값을 감싸면서 형식 검증(`@` 포함, 최대 길이 등)을 보장하고, 두 `Email`이 같은 문자열이면 동일한 객체로 판별하는 값 등치입니다. 반면 `OrderStatus` Value Object는 성격이 전혀 다릅니다. `Pending`, `Confirmed`, `Shipped`, `Delivered` 같은 유한한 상태 집합을 표현하며, 핵심은 `Pending → Confirmed`은 허용하지만 `Shipped → Pending`은 차단하는 상태 전이 규칙과, `switch` 표현식으로 모든 상태를 빠짐없이 처리하는 exhaustive 패턴 매칭입니다.

`Email`에 필요한 것은 `Equals`/`GetHashCode` 재정의와 생성자 유효성 검증이고, `OrderStatus`에 필요한 것은 sealed 계층 구조와 C# 패턴 매칭입니다. 하나의 기반 타입으로 두 요구사항을 모두 만족시키려면, `Email`에 불필요한 sealed 계층을 강제하거나 `OrderStatus`에서 패턴 매칭 대신 if-else 분기를 사용해야 하여 한쪽의 표현력이 희생됩니다.

## 검토한 옵션

1. Class 계층 + Record 계층 병행
2. 단일 Class 계층
3. 단일 Record 계층
4. Interface only (구현 없는 인터페이스)

## 결정

**선택한 옵션: "Class 계층 + Record 계층 병행"**, `Email` 같은 값 래핑 VO와 `OrderStatus` 같은 상태 집합 VO는 요구하는 언어 기능이 근본적으로 다르므로, 각각의 강점을 최대로 활용하는 전용 계층을 제공하기 위해서입니다.

**Class 계층** (전통적 Value Object):
- `AbstractValueObject` → `ValueObject` → `SimpleValueObject<T>` / `ComparableSimpleValueObject<T>`
- 등치 비교(`Equals`, `GetHashCode`)가 기반 클래스에서 처리됩니다.
- `SimpleValueObject<T>`는 단일 값 래핑, `ComparableSimpleValueObject<T>`는 비교 가능한 값 래핑에 사용됩니다.

**Record 계층** (Discriminated Union):
- `UnionValueObject<TSelf>`
- C# record의 구조적 등치와 `with` 표현식을 활용합니다.
- sealed record 상속으로 유한한 상태 집합을 표현합니다.

### 결과

- Good, because `Email`, `Money` 같은 값 래핑 VO는 Class 계층에서 `Equals`/`GetHashCode` 자동 처리를, `OrderStatus` 같은 상태 VO는 Record 계층에서 패턴 매칭을 각각 최적으로 활용합니다.
- Good, because `SimpleValueObject<T>`가 `Equals`, `GetHashCode`, `ToString`, 비교 연산자를 기반 클래스에서 일괄 처리하여 구현체마다 반복되던 등치 비교 코드가 제거됩니다.
- Good, because `UnionValueObject<TSelf>` 기반의 sealed record 계층에서 C# `switch` 표현식을 사용하면 새로운 상태 추가 시 처리하지 않은 케이스가 컴파일 경고로 표시됩니다.
- Bad, because "이 VO는 Class 계층인가 Record 계층인가"를 판단해야 하므로 선택 기준 문서("값을 감싸는가, 상태 집합을 표현하는가")를 팀 내 공유해야 합니다.

### 확인

- Value Object가 두 계층 중 하나를 반드시 상속하는지 아키텍처 규칙 테스트로 확인합니다.
- Union Value Object의 sealed record 계층이 완전한 상태 집합을 표현하는지 코드 리뷰에서 점검합니다.

## 옵션별 장단점

### Class 계층 + Record 계층 병행

- Good, because `Email`은 `SimpleValueObject<string>`을 상속하여 등치/검증을 얻고, `OrderStatus`는 `UnionValueObject<OrderStatus>`를 상속하여 패턴 매칭을 얻어 각각 최적의 C# 기능을 활용합니다.
- Good, because Class 계층은 등치/비교만, Record 계층은 sealed 상속/패턴 매칭만 제공하여 각 계층의 책임이 명확하고 단순합니다.
- Good, because Record 계층에서 C# `switch` 표현식의 exhaustiveness 검사가 새 상태 추가 시 미처리 케이스를 컴파일 타임에 알려줍니다.
- Bad, because "값 래핑이면 Class, 상태 집합이면 Record"라는 선택 기준을 문서화하고 코드 리뷰에서 일관되게 적용해야 합니다.

### 단일 Class 계층

- Good, because 모든 VO가 `AbstractValueObject`를 상속하여 기반 타입 선택 판단이 불필요합니다.
- Bad, because `OrderStatus`를 Class로 구현하면 `switch` 표현식의 exhaustiveness 검사를 받을 수 없어, 새 상태 추가 시 미처리 분기를 컴파일러가 잡아주지 못합니다.
- Bad, because 유한한 상태 집합을 Class 상속으로 표현하려면 sealed 키워드 없이 상속을 수동으로 제한해야 하고, 패턴 매칭 대신 `if-else` 또는 `is` 검사를 사용해야 합니다.

### 단일 Record 계층

- Good, because C# record의 구조적 등치(`Equals`, `GetHashCode` 자동 생성)를 모든 VO에서 별도 구현 없이 활용할 수 있습니다.
- Bad, because `Money(Amount, Currency)` 같은 복합 값 타입에서 `Amount`의 소수점 자릿수 반올림 후 비교 같은 커스텀 등치 로직을 record의 자동 생성된 `Equals`로는 표현할 수 없어 별도 재정의가 필요합니다.
- Bad, because record의 `with` 표현식(`email with { Value = "new@test.com" }`)이 유효성 검증을 거치지 않은 값 변경을 허용하여 불변성 계약을 우회할 수 있습니다.

### Interface only (구현 없는 인터페이스)

- Good, because `IValueObject` 인터페이스만 정의하고 구현은 class든 record든 자유롭게 선택할 수 있습니다.
- Bad, because `Equals`, `GetHashCode`, `ToString`, 유효성 검증 등 모든 VO가 필요로 하는 공통 로직을 구현체마다 반복 작성해야 하며, 구현 누락이 런타임 버그로 이어집니다.
- Bad, because 인터페이스만으로는 "모든 VO는 불변이고 값 등치를 보장한다"는 규약을 강제할 수 없어, 일부 VO에서 등치 비교가 누락되거나 가변 상태가 노출될 수 있습니다.

## 관련 정보

- 관련 커밋: `5c347e54`
- 관련 스펙: `spec/02-value-object`
- 관련 튜토리얼: `Docs.Site/src/content/docs/tutorials/functional-valueobject/`
