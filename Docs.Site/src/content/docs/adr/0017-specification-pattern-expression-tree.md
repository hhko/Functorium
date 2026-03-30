---
title: "ADR-0017: Specification 패턴과 Expression Tree 기반 쿼리 변환"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

"활성 상태이면서 골드 등급 이상인 고객"이라는 비즈니스 규칙이 있다고 가정합니다. 도메인 모델의 `Customer.IsEligibleForPromotion()` 메서드에 이 조건을 구현하고, Repository의 LINQ Where 절에도 `c => c.IsActive && c.Grade >= Grade.Gold`로 동일한 조건을 별도 작성합니다. 이후 등급 기준이 실버로 변경되었을 때 도메인 메서드는 수정했지만 LINQ 쿼리는 누락하여, 프로모션 대상 조회와 도메인 검증이 서로 다른 고객 집합을 반환하는 버그가 운영 환경에서 발견됩니다.

이처럼 동일한 비즈니스 규칙이 도메인 코드와 쿼리 코드에 분산되면 규칙 변경 시 한쪽 수정 누락이 사일런트 불일치로 이어집니다. 또한 "활성 AND 골드 이상" 같은 단순 조건을 넘어, "활성 AND (골드 이상 OR VIP)" 같은 복합 조건을 선언적으로 합성할 수 있는 구조가 필요합니다.

## 검토한 옵션

1. ExpressionSpecification\<T\> + PropertyMap 브릿지
2. 직접 LINQ Where 절 작성
3. Dynamic LINQ 라이브러리
4. 쿼리 객체 패턴 (Query Object)

## 결정

**선택한 옵션: "ExpressionSpecification\<T\> + PropertyMap 브릿지"**, 비즈니스 규칙의 단일 원천(Single Source of Truth)을 확보하기 위해서입니다. 하나의 Specification 클래스가 Expression Tree로 규칙을 정의하면, 도메인 검증에서는 `IsSatisfiedBy()`로 인메모리 평가하고 Repository에서는 동일한 Expression을 EF Core가 SQL로 변환합니다. 규칙이 변경되면 Specification 한 곳만 수정하면 양쪽에 즉시 반영됩니다.

`&`(AND), `|`(OR), `!`(NOT) 연산자 오버로딩으로 `ActiveSpec & (GoldOrHigherSpec | VipSpec)` 같은 복합 조건을 선언적으로 합성할 수 있으며, PropertyMap 브릿지가 도메인 속성명과 DB 컬럼명 간 차이를 흡수하여 도메인 모델의 순수성을 유지합니다.

### 결과

- Good, because 도메인 검증, 쿼리 필터, API 응답 필터 등 여러 곳에서 동일 Specification을 재사용하므로 규칙 변경 시 한 곳만 수정하면 됩니다.
- Good, because `ActiveSpec & GoldOrHigherSpec | !SuspendedSpec` 같은 합성이 비즈니스 의도를 코드에서 읽히는 그대로 표현합니다.
- Good, because PropertyMap 브릿지가 도메인의 `Grade` 속성과 DB의 `customer_grade` 컬럼 같은 명명 차이를 한 곳에서 선언적으로 해소합니다.
- Bad, because Expression Tree 내부의 `ParameterReplacer`, `ExpressionVisitor` 조합 로직이 일반 코드보다 디버깅이 어렵고, 잘못된 Expression 합성이 런타임 `InvalidOperationException`으로 나타납니다.
- Bad, because 도메인 모델과 영속성 모델의 속성이 다른 Specification마다 PropertyMap을 별도 작성해야 합니다.

### 확인

- Specification 합성(`&`, `|`, `!`)이 올바른 Expression을 생성하는지 단위 테스트로 확인합니다.
- PropertyMap을 통한 DB 쿼리 변환이 실제 SQL로 정상 변환되는지 통합 테스트로 검증합니다.

## 옵션별 장단점

### ExpressionSpecification\<T\> + PropertyMap 브릿지

- Good, because 규칙 변경 시 Specification 클래스 한 곳만 수정하면 도메인 검증과 DB 쿼리 양쪽에 즉시 반영됩니다.
- Good, because Expression Tree를 EF Core가 SQL WHERE 절로 직접 변환하므로 인메모리 필터링 없이 DB 수준에서 필터링됩니다.
- Good, because `spec1 & spec2 | !spec3` 같은 연산자 오버로딩이 비즈니스 규칙 합성을 자연어에 가깝게 표현합니다.
- Good, because PropertyMap이 도메인 모델과 영속성 모델 간 속성명/타입 차이를 Specification 외부에서 선언적으로 해소합니다.
- Bad, because `AndSpecification`, `OrSpecification` 등 Expression 조합 시 `ParameterExpression` 교체 로직이 복잡하여 초기 프레임워크 구현 비용이 높습니다.
- Bad, because 도메인 속성과 DB 컬럼이 다른 Specification마다 PropertyMap을 추가로 정의해야 하는 보일러플레이트가 발생합니다.

### 직접 LINQ Where 절 작성

- Good, because 별도 추상화 없이 `.Where(c => c.IsActive && c.Grade >= Grade.Gold)`를 바로 작성할 수 있어 학습 비용이 없습니다.
- Bad, because 동일한 `IsActive && Grade >= Gold` 조건이 도메인 메서드와 Repository LINQ에 각각 존재하여, 한쪽 수정 누락 시 사일런트 불일치가 발생합니다.
- Bad, because 규칙이 여러 곳에 분산된 경우 변경 영향 범위를 전체 코드 검색으로만 파악할 수 있습니다.
- Bad, because 복합 조건을 합성하려면 매번 새로운 Where 절을 수작업으로 조합해야 하며, 재사용 가능한 구조가 없습니다.

### Dynamic LINQ 라이브러리

- Good, because `"Age > 18 AND IsActive"` 같은 문자열로 동적 쿼리를 런타임에 구성할 수 있어 유연합니다.
- Bad, because `"Age"` 속성을 `"UserAge"`로 리네이밍하면 컴파일은 성공하지만 런타임에 `ParseException`이 발생하여 타입 안전성이 없습니다.
- Bad, because 문자열 `"Actve"`(오타) 같은 실수가 특정 분기에서만 런타임 오류로 나타나 발견이 늦어집니다.
- Bad, because 문자열 쿼리는 도메인 레이어의 비즈니스 규칙과 별개이므로 규칙 중복 문제가 해소되지 않습니다.

### 쿼리 객체 패턴 (Query Object)

- Good, because 쿼리 로직을 `ActiveCustomerQuery` 같은 객체로 캡슐화하여 Repository 간 재사용할 수 있습니다.
- Bad, because 쿼리 객체가 Expression Tree를 직접 생성하지 않으므로, EF Core의 SQL 변환과 통합하려면 별도 변환 계층을 추가로 구현해야 합니다.
- Bad, because 쿼리 객체는 DB 조회 전용이므로 도메인 레이어의 인메모리 검증에는 사용할 수 없어 규칙 중복 문제가 그대로 남습니다.

## 관련 정보

- 관련 커밋: `f1dec480`
- 관련 튜토리얼: `Docs.Site/src/content/docs/tutorials/specification-pattern/`
- 참고: Eric Evans, Domain-Driven Design — Chapter 9, Specification pattern
