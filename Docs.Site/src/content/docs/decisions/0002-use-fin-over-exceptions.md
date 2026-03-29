---
title: "ADR-0002: 예외 대신 Fin 타입으로 실패 표현"
status: "accepted"
date: 2026-03-26
---

## 맥락과 문제

비즈니스 로직에서 발생하는 실패(검증 실패, 상태 전이 불가 등)를 예외(Exception)로 처리하면 메서드 시그니처에 실패 가능성이 드러나지 않습니다. 호출자는 어떤 예외가 발생할 수 있는지 코드를 읽거나 문서를 참조해야 하며, try-catch 블록이 비즈니스 흐름을 가리고 합성(composition)을 방해합니다.

함수형 프로그래밍에서는 실패를 반환 타입에 명시하여 컴파일러가 실패 처리를 강제하고, LINQ 합성으로 비즈니스 파이프라인을 선언적으로 표현합니다. Functorium은 단일 값 실패, 병렬 검증, 사이드 이펙트를 포함하는 실패 등 다양한 시나리오에 대응할 수 있는 타입 체계가 필요합니다.

## 검토한 옵션

1. Fin/Validation/FinT 타입 체계 (LanguageExt)
2. Result 타입 직접 구현
3. FluentResults 라이브러리
4. ErrorOr 라이브러리
5. 예외 유지 (현상 유지)

## 결정

**선택한 옵션: "Fin/Validation/FinT 타입 체계 (LanguageExt)"**, `Fin<T>`로 단일 실패, `Validation<Error, T>`로 병렬 검증 실패, `FinT<IO, T>`로 사이드 이펙트 포함 실패를 표현하여 시나리오별 최적 타입을 제공하기 때문입니다.

### 결과

- Good, because 메서드 시그니처에 실패 가능성이 명시되어 컴파일 타임에 실패 처리가 강제됩니다.
- Good, because LINQ 합성으로 비즈니스 파이프라인을 선언적으로 표현할 수 있습니다.
- Good, because `ApplyT`로 병렬 검증 결과를 합성하여 모든 오류를 한 번에 수집합니다.
- Bad, because LanguageExt 학습 곡선이 존재하며, 기존 .NET 개발자에게 낯선 패턴입니다.
- Bad, because System.Text.Json이 `Seq<T>` 등 LanguageExt 타입을 기본 지원하지 않아 어댑터 계층에서 변환이 필요합니다.

### 확인

- `Fin<T>` 반환 핸들러가 `ThrowIfFail()` 또는 패턴 매칭으로 실패를 처리하는지 확인합니다.
- 검증 로직이 `Validation<Error, T>`를 사용하여 병렬 오류 수집을 하는지 확인합니다.
- 예외를 throw하는 비즈니스 로직이 없는지 아키텍처 테스트로 검증합니다.

## 옵션별 장단점

### Fin/Validation/FinT 타입 체계 (LanguageExt)

- Good, because 단일 실패(`Fin<T>`), 병렬 검증(`Validation<Error, T>`), 사이드 이펙트(`FinT<IO, T>`) 시나리오를 타입으로 구분합니다.
- Good, because LINQ 쿼리 구문과 `Bind`/`Map`/`ApplyT`로 합성 가능합니다.
- Good, because `Unwrap()`, `ThrowIfFail()` 등 탈출 해치가 있어 점진적 도입이 가능합니다.
- Bad, because LanguageExt에 대한 의존이 프레임워크 전반에 퍼집니다.

### Result 타입 직접 구현

- Good, because 외부 의존 없이 간단한 성공/실패를 표현할 수 있습니다.
- Bad, because LINQ 합성, 병렬 검증, IO 모나드 트랜스포머를 직접 구현해야 합니다.
- Bad, because 유지보수 부담이 지속적으로 발생합니다.

### FluentResults 라이브러리

- Good, because .NET 생태계에서 널리 사용되어 익숙합니다.
- Bad, because 함수형 합성(Bind, Map, LINQ)을 지원하지 않습니다.
- Bad, because 타입 안전한 오류 분류가 어렵습니다.

### ErrorOr 라이브러리

- Good, because 경량이고 학습 곡선이 낮습니다.
- Bad, because LINQ 합성을 지원하지 않습니다.
- Bad, because 병렬 검증(Validation applicative)이 없습니다.

### 예외 유지 (현상 유지)

- Good, because 추가 학습이나 변경이 필요 없습니다.
- Bad, because 시그니처에 실패 가능성이 드러나지 않아 런타임에서야 발견됩니다.
- Bad, because try-catch가 비즈니스 흐름을 가리고 합성을 방해합니다.

## 관련 정보

- 관련 커밋: `b967b91c` refactor(validation): Fin<T>.Unwrap() 도입 및 핸들러 ThrowIfFail 전환
- 관련 커밋: `47d88180` feat(validation): FinApplyExtensions.ApplyT 추가 및 CreateProductCommand 참조 구현
- 관련 커밋: `3cb5c29b` feat(domain): Fin 튜플 Apply 확장 메서드 추가
- 관련 문서: `Docs.Site/src/content/docs/guides/domain/`
