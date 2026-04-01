---
title: "ADR-0001: Foundation - 예외 대신 Fin 타입으로 실패 표현"
status: "accepted"
date: 2026-03-26
---

## 맥락과 문제

주문 생성 핸들러를 작성한다고 가정합니다. 재고 부족, 가격 변경, 결제 한도 초과 중 어떤 실패가 발생할 수 있는지 `PlaceOrder(command)` 시그니처만으로는 알 수 없습니다. 호출자는 소스 코드를 열어 throw 구문을 하나하나 추적하거나 문서를 뒤져야 하고, try-catch를 중첩하면 "재고 확인 → 가격 검증 → 결제 요청 → 주문 확정"이라는 비즈니스 흐름이 예외 처리 분기 속에 파묻힙니다. 새로운 실패 유형이 추가되어도 컴파일러는 아무런 경고를 주지 않으므로, 처리되지 않은 예외는 운영 환경 런타임에서야 발견됩니다.

Functorium은 이 문제를 타입 수준에서 해결해야 합니다. 단일 값 실패(재고 부족으로 주문 불가), 병렬 검증 실패(상품명/가격/카테고리를 한 번에 검증하여 모든 오류를 수집), 사이드 이펙트를 포함하는 실패(외부 결제 API 호출 후 타임아웃)까지 시나리오별로 대응할 수 있는 타입 체계가 필요합니다.

## 검토한 옵션

1. Fin/Validation/FinT 타입 체계 (LanguageExt)
2. Result 타입 직접 구현
3. FluentResults 라이브러리
4. ErrorOr 라이브러리
5. 예외 유지 (현상 유지)

## 결정

**선택한 옵션: "Fin/Validation/FinT 타입 체계 (LanguageExt)"**. 재고 부족처럼 단일 원인으로 실패하는 경로는 `Fin<T>`로, 상품 생성처럼 여러 필드를 동시에 검증하여 모든 오류를 한 번에 돌려줘야 하는 경로는 `Validation<Error, T>`로, 외부 결제 API처럼 사이드 이펙트를 동반하는 실패는 `FinT<IO, T>`로 표현합니다. 세 가지 시나리오를 타입 하나로 뭉뚱그리지 않고, 각각에 최적화된 타입을 제공하기 때문입니다.

### 결과

- <span class="adr-good">Good</span>, because `Fin<Order>` 반환 타입만 보고도 이 핸들러가 실패할 수 있음을 알 수 있어, 실패 처리 누락이 컴파일 오류로 드러납니다.
- <span class="adr-good">Good</span>, because `Bind`/`Map` 체인과 LINQ 쿼리 구문으로 "재고 확인 → 가격 검증 → 결제 → 주문 확정" 흐름을 try-catch 없이 선언적으로 합성합니다.
- <span class="adr-good">Good</span>, because `ApplyT`로 상품명/가격/카테고리 검증 결과를 병렬 합성하여 사용자에게 모든 오류를 한 번에 반환합니다.
- <span class="adr-bad">Bad</span>, because `Bind`, `Map`, `FinT` 같은 함수형 개념에 익숙하지 않은 C# 개발자는 코드 리뷰와 디버깅에서 진입 장벽을 느낍니다.
- <span class="adr-bad">Bad</span>, because System.Text.Json이 `Seq<T>` 등 LanguageExt 컬렉션 타입을 역직렬화하지 못하므로, API 응답 DTO에서 `List<T>`로 변환하는 어댑터 코드가 필요합니다.

### 확인

- `Fin<T>` 반환 핸들러가 `ThrowIfFail()` 또는 패턴 매칭으로 실패를 처리하는지 확인합니다.
- 검증 로직이 `Validation<Error, T>`를 사용하여 병렬 오류 수집을 하는지 확인합니다.
- 예외를 throw하는 비즈니스 로직이 없는지 아키텍처 테스트로 검증합니다.

## 옵션별 장단점

### Fin/Validation/FinT 타입 체계 (LanguageExt)

- <span class="adr-good">Good</span>, because 단일 실패(`Fin<T>`), 병렬 검증(`Validation<Error, T>`), 사이드 이펙트(`FinT<IO, T>`)를 시나리오별 전용 타입으로 구분하여 의도가 코드에 드러납니다.
- <span class="adr-good">Good</span>, because LINQ 쿼리 구문과 `Bind`/`Map`/`ApplyT`로 비즈니스 단계를 합성할 수 있어 파이프라인 가독성이 높습니다.
- <span class="adr-good">Good</span>, because `Unwrap()`, `ThrowIfFail()` 등 탈출 해치가 있어 기존 예외 기반 코드에서 점진적으로 전환할 수 있습니다.
- <span class="adr-bad">Bad</span>, because `Fin<T>`가 Domain, Application, Adapter 전 레이어에 전파되어 LanguageExt에 대한 구조적 의존이 형성됩니다.

### Result 타입 직접 구현

- <span class="adr-good">Good</span>, because 외부 의존 없이 `Result<T, E>` 수준의 성공/실패를 표현할 수 있습니다.
- <span class="adr-bad">Bad</span>, because `SelectMany`(LINQ 합성), `ApplyT`(병렬 검증), IO 모나드 트랜스포머를 모두 직접 구현하고 테스트해야 하며, 이는 사실상 LanguageExt의 일부를 재작성하는 것과 같습니다.
- <span class="adr-bad">Bad</span>, because 새로운 합성 시나리오가 추가될 때마다 확장 메서드를 계속 추가해야 하므로 유지보수 부담이 누적됩니다.

### FluentResults 라이브러리

- <span class="adr-good">Good</span>, because .NET 생태계에서 널리 사용되어 팀 합류 시 학습 부담이 적습니다.
- <span class="adr-bad">Bad</span>, because `Bind`/`Map`/LINQ 합성을 지원하지 않아, 여러 단계를 연결하려면 결국 if-else 분기로 회귀합니다.
- <span class="adr-bad">Bad</span>, because 오류가 문자열 기반이어서 `DomainErrorType.InsufficientStock` 같은 타입 안전한 분류와 패턴 매칭이 불가능합니다.

### ErrorOr 라이브러리

- <span class="adr-good">Good</span>, because API가 직관적이고 경량이어서 30분 이내에 도입할 수 있습니다.
- <span class="adr-bad">Bad</span>, because LINQ 쿼리 구문을 지원하지 않아, 3단계 이상의 비즈니스 파이프라인을 선언적으로 합성할 수 없습니다.
- <span class="adr-bad">Bad</span>, because Applicative 합성(병렬 검증)이 없어, 상품 생성처럼 여러 필드를 동시에 검증하고 모든 오류를 수집하는 패턴을 구현할 수 없습니다.

### 예외 유지 (현상 유지)

- <span class="adr-good">Good</span>, because 기존 C# 코드 스타일을 그대로 유지하므로 추가 학습이 필요 없습니다.
- <span class="adr-bad">Bad</span>, because 새로 추가된 예외 유형을 호출자가 catch하지 않아도 컴파일이 통과하므로, 처리되지 않은 실패가 운영 환경에서 500 에러로 노출됩니다.
- <span class="adr-bad">Bad</span>, because try-catch 중첩이 비즈니스 흐름을 가리고, 검증 → 처리 → 저장 단계를 `Bind`/`Map`으로 합성하는 것이 구조적으로 불가능합니다.

## 관련 정보

- 관련 커밋: `b967b91c` refactor(validation): Fin<T>.Unwrap() 도입 및 핸들러 ThrowIfFail 전환
- 관련 커밋: `47d88180` feat(validation): FinApplyExtensions.ApplyT 추가 및 CreateProductCommand 참조 구현
- 관련 커밋: `3cb5c29b` feat(domain): Fin 튜플 Apply 확장 메서드 추가
- 관련 문서: `Docs.Site/src/content/docs/guides/domain/`
