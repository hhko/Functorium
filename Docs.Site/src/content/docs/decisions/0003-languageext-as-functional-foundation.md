---
title: "ADR-0003: LanguageExt를 함수형 기반 라이브러리로 채택"
status: "accepted"
date: 2026-03-15
---

## 맥락과 문제

Functorium은 C# 위에서 함수형 프로그래밍 패턴을 적용하여 비즈니스 로직의 합성 가능성과 부수 효과 제어를 달성하려 합니다. .NET 생태계에는 여러 함수형 라이브러리가 존재하며, 각각 지원하는 추상화 수준과 범위가 다릅니다.

프레임워크 수준에서 선택한 함수형 라이브러리는 모든 레이어에 영향을 미치므로, Fin/Validation 같은 오류 처리 타입뿐 아니라 IO 모나드, 모나드 트랜스포머, LINQ 합성, Timeout/Retry/Fork/Bracket 같은 이펙트 제어까지 종합적으로 지원해야 합니다.

## 검토한 옵션

1. LanguageExt
2. CSharpFunctionalExtensions
3. OneOf
4. 직접 구현

## 결정

**선택한 옵션: "LanguageExt"**, Fin, IO, FinT(모나드 트랜스포머), LINQ 합성, Timeout/Retry/Fork/Bracket 등 프레임워크가 요구하는 함수형 추상화를 가장 폭넓게 제공하기 때문입니다.

### 결과

- Good, because 오류 처리(Fin/Validation), 부수 효과(IO), 이펙트 합성(FinT)을 단일 라이브러리로 통합합니다.
- Good, because LINQ 쿼리 구문으로 비즈니스 파이프라인을 선언적으로 작성할 수 있습니다.
- Good, because Timeout, Retry, Fork, Bracket 등 인프라 관심사를 타입 안전하게 표현합니다.
- Bad, because 학습 곡선이 가파르고 Haskell에서 차용한 개념이 많아 C# 개발자에게 낯설 수 있습니다.
- Bad, because 라이브러리가 대규모이며 사용하지 않는 기능도 의존에 포함됩니다.

### 확인

- 핵심 패키지(`Functorium.Core`, `Functorium.Application` 등)가 LanguageExt를 참조하는지 확인합니다.
- IO 모나드를 통해 부수 효과가 지연 실행되고 합성되는지 파이프라인 테스트로 검증합니다.

## 옵션별 장단점

### LanguageExt

- Good, because Fin, Validation, Option, Either, IO, FinT 등 풍부한 타입을 제공합니다.
- Good, because LINQ SelectMany를 구현하여 쿼리 구문으로 모나딕 합성이 가능합니다.
- Good, because IO 모나드로 부수 효과를 순수 함수처럼 합성하고 Timeout/Retry/Fork/Bracket을 타입 안전하게 처리합니다.
- Bad, because 라이브러리 규모가 크고 breaking change가 메이저 버전에서 발생할 수 있습니다.
- Bad, because .NET 생태계에서 LanguageExt를 아는 개발자가 상대적으로 적습니다.

### CSharpFunctionalExtensions

- Good, because 학습 곡선이 낮고 .NET 커뮤니티에서 널리 사용됩니다.
- Good, because Result, Maybe, ValueObject 등 DDD에 유용한 타입을 제공합니다.
- Bad, because IO 모나드를 지원하지 않아 부수 효과 제어가 불가능합니다.
- Bad, because 모나드 트랜스포머가 없어 복합 이펙트 합성이 어렵습니다.

### OneOf

- Good, because 경량 discriminated union으로 간단한 패턴 매칭이 가능합니다.
- Bad, because LINQ 합성을 지원하지 않습니다.
- Bad, because Bind/Map 등 모나딕 연산이 없어 파이프라인 합성이 불가능합니다.
- Bad, because 오류 타입을 구조적으로 분류하는 체계가 없습니다.

### 직접 구현

- Good, because 필요한 기능만 경량으로 구현할 수 있습니다.
- Good, because 외부 의존이 없습니다.
- Bad, because IO 모나드, 모나드 트랜스포머, LINQ 합성을 직접 구현하고 유지보수해야 합니다.
- Bad, because 버그 가능성이 높고 커뮤니티 검증이 없습니다.

## 관련 정보

- 관련 커밋: `cda0a338` feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
- 관련 커밋: `d304ab40` refactor(ecommerce-ddd): PlaceOrderCommand Handle 메서드 함수형 리팩터링
- 관련 문서: `Docs.Site/src/content/docs/spec/`
