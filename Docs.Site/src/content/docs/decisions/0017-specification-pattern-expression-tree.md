---
title: "ADR-0017: Specification 패턴과 Expression Tree 기반 쿼리 변환"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

비즈니스 규칙을 도메인 레이어에서 캡슐화하되, 동일한 규칙을 데이터베이스 쿼리로도 변환할 수 있어야 합니다. 예를 들어 "활성 상태이면서 특정 등급 이상인 고객" 같은 조건은 도메인 검증과 DB 조회 양쪽에서 동일하게 적용되어야 합니다.

기존 방식에서는 LINQ Where 절에 조건을 직접 작성하여 동일한 비즈니스 규칙이 도메인 코드와 쿼리 코드에 중복되고, 규칙 변경 시 양쪽을 모두 수정해야 하는 문제가 있었습니다. 또한 복합 조건(AND, OR, NOT)을 유연하게 합성할 수 있는 구조가 필요합니다.

## 검토한 옵션

1. ExpressionSpecification\<T\> + PropertyMap 브릿지
2. 직접 LINQ Where 절 작성
3. Dynamic LINQ 라이브러리
4. 쿼리 객체 패턴 (Query Object)

## 결정

**선택한 옵션: "ExpressionSpecification\<T\> + PropertyMap 브릿지"**, 비즈니스 규칙을 Expression Tree로 캡슐화하여 도메인 검증과 DB 쿼리 변환을 단일 소스에서 처리하고, PropertyMap으로 도메인 속성과 DB 컬럼 간의 매핑을 분리할 수 있기 때문입니다.

`&`(AND), `|`(OR), `!`(NOT) 연산자 오버로딩을 통해 Specification을 자유롭게 합성할 수 있으며, Expression Tree 기반이므로 EF Core 등 ORM이 SQL로 변환할 수 있습니다.

### 결과

- Good, because 비즈니스 규칙이 단일 Specification 클래스에 캡슐화되어 재사용됩니다.
- Good, because `&`, `|`, `!` 연산자로 복합 조건을 선언적으로 합성할 수 있습니다.
- Good, because PropertyMap 브릿지가 도메인 모델과 영속성 모델의 속성 차이를 흡수합니다.
- Bad, because Expression Tree 디버깅이 일반 코드보다 어렵습니다.
- Bad, because PropertyMap 작성이 추가적인 보일러플레이트가 됩니다.

### 확인

- Specification 합성(`&`, `|`, `!`)이 올바른 Expression을 생성하는지 단위 테스트로 확인합니다.
- PropertyMap을 통한 DB 쿼리 변환이 실제 SQL로 정상 변환되는지 통합 테스트로 검증합니다.

## 옵션별 장단점

### ExpressionSpecification\<T\> + PropertyMap 브릿지

- Good, because 비즈니스 규칙을 단일 클래스에 캡슐화하여 도메인과 쿼리에서 재사용합니다.
- Good, because Expression Tree 기반이므로 ORM이 SQL로 변환할 수 있습니다.
- Good, because 연산자 오버로딩으로 직관적인 합성 문법을 제공합니다.
- Good, because PropertyMap이 도메인 속성과 DB 컬럼 간 매핑을 명시적으로 분리합니다.
- Bad, because Expression Tree 조합 로직의 복잡도가 높습니다.
- Bad, because PropertyMap을 Specification마다 작성해야 합니다.

### 직접 LINQ Where 절 작성

- Good, because 구현이 단순하고 학습 비용이 없습니다.
- Bad, because 동일한 비즈니스 규칙이 도메인 코드와 쿼리 코드에 중복됩니다.
- Bad, because 규칙 변경 시 여러 위치를 동시에 수정해야 합니다.
- Bad, because 복합 조건 합성을 위한 표준 구조가 없습니다.

### Dynamic LINQ 라이브러리

- Good, because 문자열 기반으로 동적 쿼리를 구성할 수 있습니다.
- Bad, because 컴파일 타임 타입 안전성이 보장되지 않습니다.
- Bad, because 문자열 오타가 런타임 오류로 이어집니다.
- Bad, because 도메인 규칙과 쿼리 규칙이 여전히 분리됩니다.

### 쿼리 객체 패턴 (Query Object)

- Good, because 쿼리 로직을 객체로 캡슐화하여 재사용할 수 있습니다.
- Bad, because Expression Tree로의 변환이 내장되어 있지 않아 ORM 통합이 어렵습니다.
- Bad, because 도메인 검증 로직과의 통합이 별도로 필요합니다.

## 관련 정보

- 관련 커밋: `f1dec480`
- 관련 튜토리얼: `Docs.Site/src/content/docs/tutorials/specification-pattern/`
- 참고: Eric Evans, Domain-Driven Design — Chapter 9, Specification pattern
