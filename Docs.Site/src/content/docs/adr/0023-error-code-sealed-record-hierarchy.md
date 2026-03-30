---
title: "ADR-0023: 에러 코드 sealed record 계층 구조"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

에러 타입을 문자열로 관리하면 다음과 같은 문제가 발생합니다.

- **오타**: `"NotFound"` vs `"Notfound"` 같은 오타가 런타임까지 발견되지 않습니다.
- **중복**: 동일한 에러를 서로 다른 문자열로 표현하여 일관성이 깨집니다.
- **추적 불가**: 에러가 어느 레이어에서 발생했는지 에러 타입만으로는 판별할 수 없습니다.
- **계층 구분 불가**: 도메인 에러, 애플리케이션 에러, 어댑터 에러를 구조적으로 구분할 수 없습니다.

비즈니스 규칙 위반(도메인), 인가 실패(애플리케이션), 외부 서비스 장애(어댑터) 등 레이어별 에러 성격이 다르므로, 에러 타입 자체에 레이어 정보와 컨텍스트가 포함되어야 합니다.

## 검토한 옵션

1. 레이어별 sealed record 계층 + 에러 코드 자동 생성
2. enum 기반 에러 타입
3. 문자열 상수
4. 예외 클래스 계층
5. 단일 ErrorType (레이어 구분 없음)

## 결정

**선택한 옵션: "레이어별 sealed record 계층 + 에러 코드 자동 생성"**, 각 레이어에 맞는 sealed record 계층(`DomainErrorType` 27종, `ApplicationErrorType`, `AdapterErrorType`)을 정의하고, `IHasErrorCode` 인터페이스를 통해 `{Layer}.{Context}.{Name}` 형식의 에러 코드를 자동 생성하기 때문입니다.

- **DomainErrorType**: `NotFound`, `InvalidState`, `InvalidTransition`, `DuplicateValue` 등 27종의 도메인 에러를 sealed record로 정의합니다.
- **ApplicationErrorType**: `Unauthorized`, `Forbidden`, `Conflict` 등 애플리케이션 레이어 에러를 정의합니다.
- **AdapterErrorType**: `ExternalServiceFailure`, `DatabaseError` 등 어댑터 레이어 에러를 정의합니다.
- **에러 코드 형식**: `Domain.Order.InvalidTransition`, `Application.Auth.Unauthorized` 같은 구조화된 코드가 자동 생성됩니다.
- **팩토리**: `DomainError.For<T>()` 메서드로 에러 생성 시 타입 정보가 자동 포함됩니다.

### 결과

- Good, because 에러 타입이 컴파일 타임에 검증되어 오타와 중복이 방지됩니다.
- Good, because sealed record이므로 패턴 매칭으로 모든 에러 케이스를 exhaustive하게 처리할 수 있습니다.
- Good, because 에러 코드에 레이어와 컨텍스트가 포함되어 발생 위치를 즉시 파악할 수 있습니다.
- Good, because `DomainError.For<T>()` 팩토리가 타입 정보를 자동 주입하여 보일러플레이트를 줄입니다.
- Bad, because sealed record 계층 정의에 초기 설계 비용이 필요합니다.
- Bad, because 새로운 에러 타입 추가 시 sealed record 계층을 확장해야 합니다.

### 확인

- 모든 에러 타입이 해당 레이어의 sealed record 계층에 속하는지 아키텍처 규칙 테스트로 확인합니다.
- 에러 코드 형식이 `{Layer}.{Context}.{Name}` 패턴을 준수하는지 단위 테스트로 검증합니다.

## 옵션별 장단점

### 레이어별 sealed record 계층 + 에러 코드 자동 생성

- Good, because 컴파일 타임 타입 안전성이 보장됩니다.
- Good, because sealed 계층으로 exhaustive 패턴 매칭이 가능합니다.
- Good, because 에러 코드가 구조화되어 로깅, 모니터링, 클라이언트 응답에서 활용됩니다.
- Good, because `IHasErrorCode` 인터페이스로 레이어 간 통일된 에러 처리가 가능합니다.
- Bad, because sealed record 계층의 초기 설계와 유지보수 비용이 있습니다.

### enum 기반 에러 타입

- Good, because 구현이 단순하고 친숙합니다.
- Bad, because enum은 sealed 계층처럼 속성을 가질 수 없어 추가 정보 전달이 어렵습니다.
- Bad, because 레이어별 enum을 분리해도 타입 시스템 수준의 구분이 약합니다.
- Bad, because 확장 시 기존 enum에 항목을 추가해야 하여 Open-Closed Principle에 위배됩니다.

### 문자열 상수

- Good, because 정의가 가장 단순합니다.
- Bad, because 오타가 컴파일 타임에 발견되지 않습니다.
- Bad, because 레이어 구분, 컨텍스트 정보 등을 구조적으로 포함할 수 없습니다.
- Bad, because 리팩토링 시 문자열 검색에 의존해야 합니다.

### 예외 클래스 계층

- Good, because .NET의 기본 에러 처리 메커니즘과 일치합니다.
- Bad, because catch 기반 제어 흐름은 함수형 프로그래밍의 `Fin<T>` 패턴과 충돌합니다.
- Bad, because 예외는 성능 비용이 높고 스택 트레이스 생성 오버헤드가 있습니다.
- Bad, because ADR-0002에서 예외 대신 `Fin<T>`를 채택한 결정과 상충합니다.

### 단일 ErrorType (레이어 구분 없음)

- Good, because 에러 타입 계층이 단순합니다.
- Bad, because 도메인 에러와 인프라 에러를 구조적으로 구분할 수 없습니다.
- Bad, because 에러 처리 시 레이어별 분기가 불가능합니다.

## 관련 정보

- 관련 스펙: `spec/04-error-system`
- 관련 API: `DomainError.For<T>()` 팩토리
- 관련 ADR: [ADR-0002: 예외 대신 Fin 타입으로 실패 표현](./0002-use-fin-over-exceptions/)
