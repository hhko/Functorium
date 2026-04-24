---
title: "ADR-0002: Foundation - LanguageExt를 함수형 기반 라이브러리로 채택"
status: "accepted"
date: 2026-03-15
---

## 맥락과 문제

Functorium은 C# 위에서 함수형 프로그래밍 패턴으로 비즈니스 로직을 합성하고 부수 효과를 제어하려 합니다. 단순한 `Result<T>` 타입만으로는 "외부 결제 API 호출에 3초 타임아웃을 걸고, 실패 시 2회 재시도하며, 성공하면 다음 단계와 합성한다"는 요구를 표현할 수 없습니다. 결국 try-catch와 수동 재시도 루프로 회귀하게 되고, 비즈니스 흐름이 인프라 코드에 묻힙니다.

프레임워크 수준에서 채택하는 함수형 라이브러리는 Domain부터 Adapter까지 모든 레이어에 영향을 미칩니다. 따라서 Fin/Validation 같은 오류 처리 타입은 물론, IO 모나드로 부수 효과를 지연 실행하고, 모나드 트랜스포머(FinT)로 복합 이펙트를 합성하며, Timeout/Retry/Fork/Bracket 같은 인프라 관심사까지 타입 안전하게 다룰 수 있어야 합니다.

## 검토한 옵션

1. LanguageExt
2. CSharpFunctionalExtensions
3. OneOf
4. 직접 구현

## 결정

**선택한 옵션: "LanguageExt"**. 검토한 4개 옵션 중 오류 처리(Fin/Validation), 부수 효과 지연(IO), 복합 이펙트 합성(FinT), LINQ 쿼리 구문, 그리고 Timeout/Retry/Fork/Bracket까지 Functorium이 요구하는 모든 함수형 추상화를 단일 라이브러리로 제공하는 유일한 선택지이기 때문입니다.

### 결과

- <span class="adr-good">Good</span>, because 오류 처리(Fin/Validation), 부수 효과 제어(IO), 복합 이펙트 합성(FinT)을 별도 라이브러리 조합 없이 하나로 통합하여 API 일관성을 유지합니다.
- <span class="adr-good">Good</span>, because LINQ 쿼리 구문으로 "검증 → 도메인 로직 → 영속 → 이벤트 발행" 파이프라인을 선언적으로 작성할 수 있어 비즈니스 흐름이 코드에 그대로 드러납니다.
- <span class="adr-good">Good</span>, because Timeout, Retry, Fork, Bracket 등 인프라 관심사를 IO 타입 위에서 합성하므로, try-catch와 수동 재시도 루프가 제거됩니다.
- <span class="adr-bad">Bad</span>, because `FinT`, `Eff`, `Aff` 등 Haskell에서 차용한 개념이 많아, 함수형 프로그래밍 경험이 없는 C# 개발자는 모나드 트랜스포머 개념을 익히는 데 상당한 학습 시간이 필요합니다.
- <span class="adr-bad">Bad</span>, because LanguageExt는 수백 개의 타입과 확장 메서드를 포함하는 대규모 라이브러리이며, Functorium이 사용하지 않는 Either, Eff 등도 의존에 포함됩니다.

### 확인

- 핵심 패키지(`Functorium.Core`, `Functorium.Application` 등)가 LanguageExt를 참조하는지 확인합니다.
- IO 모나드를 통해 부수 효과가 지연 실행되고 합성되는지 파이프라인 테스트로 검증합니다.

## 옵션별 장단점

### LanguageExt

- <span class="adr-good">Good</span>, because Fin, Validation, Option, Either, IO, FinT 등 Functorium의 모든 레이어가 필요로 하는 타입을 단일 패키지에서 제공합니다.
- <span class="adr-good">Good</span>, because LINQ `SelectMany`를 구현하므로, `from ... in ... select` 구문으로 모나딕 합성을 C# 개발자에게 익숙한 형태로 표현할 수 있습니다.
- <span class="adr-good">Good</span>, because IO 모나드 위에서 Timeout/Retry/Fork/Bracket을 타입 안전하게 합성하여, 인프라 관심사가 비즈니스 로직을 침범하지 않습니다.
- <span class="adr-bad">Bad</span>, because 메이저 버전 업그레이드 시 breaking change가 발생할 수 있어, 프레임워크 전체에 영향을 미치는 마이그레이션 비용이 큽니다.
- <span class="adr-bad">Bad</span>, because .NET 생태계에서 LanguageExt 경험자가 드물어 채용 풀이 제한되고, 신규 팀원 온보딩 시 추가 교육이 필요합니다.

### CSharpFunctionalExtensions

- <span class="adr-good">Good</span>, because Result, Maybe, ValueObject 등 DDD에 친숙한 타입을 제공하며, 학습 곡선이 낮아 팀 도입이 빠릅니다.
- <span class="adr-good">Good</span>, because NuGet 다운로드 수가 많아 커뮤니티 지원과 레퍼런스가 풍부합니다.
- <span class="adr-bad">Bad</span>, because IO 모나드가 없어 "결제 API 호출 후 실패 시 재시도"같은 부수 효과를 타입 안전하게 합성할 수 없고, 결국 try-catch로 회귀합니다.
- <span class="adr-bad">Bad</span>, because 모나드 트랜스포머가 없어 `Fin` + `IO` 같은 복합 이펙트를 하나의 파이프라인으로 합성하는 것이 구조적으로 불가능합니다.

### OneOf

- <span class="adr-good">Good</span>, because 경량 discriminated union으로 `Match`를 통한 패턴 매칭이 가능하고 도입이 간단합니다.
- <span class="adr-bad">Bad</span>, because LINQ 합성을 지원하지 않아, 다단계 비즈니스 파이프라인을 선언적으로 구성할 수 없습니다.
- <span class="adr-bad">Bad</span>, because `Bind`/`Map` 등 모나딕 연산이 없어, 각 단계의 결과를 다음 단계에 연결하려면 수동 분기가 필요합니다.
- <span class="adr-bad">Bad</span>, because `OneOf<T0, T1, T2>` 형태의 위치 기반 타입 파라미터로는 `DomainErrorKind` 같은 구조적 오류 분류 체계를 표현할 수 없습니다.

### 직접 구현

- <span class="adr-good">Good</span>, because Functorium이 실제로 사용하는 타입만 경량으로 구현하여 불필요한 의존을 제거할 수 있습니다.
- <span class="adr-good">Good</span>, because 외부 라이브러리 버전 업그레이드에 따른 breaking change 위험이 없습니다.
- <span class="adr-bad">Bad</span>, because IO 모나드, 모나드 트랜스포머(`FinT`), LINQ `SelectMany` 통합을 직접 구현하고 검증해야 하며, 이는 수천 줄 규모의 핵심 인프라 코드를 자체 유지보수하는 것을 의미합니다.
- <span class="adr-bad">Bad</span>, because 엣지 케이스(스레드 안전성, 스택 오버플로우 방지 등)에서 버그가 발생할 수 있고, 커뮤니티 검증 없이 신뢰성을 확보하기 어렵습니다.

## 관련 정보

- 관련 커밋: `cda0a338` feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
- 관련 커밋: `d304ab40` refactor(ecommerce-ddd): PlaceOrderCommand Handle 메서드 함수형 리팩터링
- 관련 문서: `Docs.Site/src/content/docs/spec/`
