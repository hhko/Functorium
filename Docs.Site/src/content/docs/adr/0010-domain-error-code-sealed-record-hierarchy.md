---
title: "ADR-0010: Domain - 에러 코드 sealed record 계층 구조"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

API 응답에서 `"NotFound"` 에러가 반환되었다고 가정합니다. 이것이 주문이 존재하지 않아서 발생한 도메인 에러인지, 외부 결제 서비스가 404를 반환한 어댑터 에러인지, 에러 문자열만으로는 구분할 수 없습니다. 모니터링 대시보드에서 `"NotFound"` 발생 건수를 집계해도 도메인 문제와 인프라 문제가 뒤섞여 의미 있는 분석이 불가능합니다.

문자열 기반 에러 관리의 문제는 이에 그치지 않습니다. 한 개발자가 `"NotFound"`로 작성하고 다른 개발자가 `"Notfound"`로 작성하면, 동일한 에러가 서로 다른 문자열로 표현되어 일관성이 깨집니다. 이런 오타는 컴파일 타임에 발견되지 않고, 특정 에러를 처리하는 `switch` 분기에서 매칭 실패로 나타나 런타임까지 잠복합니다. 비즈니스 규칙 위반(도메인), 인가 실패(애플리케이션), 외부 서비스 장애(어댑터)는 성격이 다른 에러이므로, 에러 타입 자체에 레이어 정보와 발생 컨텍스트가 구조적으로 포함되어야 합니다.

## 검토한 옵션

1. 레이어별 sealed record 계층 + 에러 코드 자동 생성
2. enum 기반 에러 타입
3. 문자열 상수
4. 예외 클래스 계층
5. 단일 ErrorType (레이어 구분 없음)

## 결정

**선택한 옵션: "레이어별 sealed record 계층 + 에러 코드 자동 생성"**, 에러를 문자열이 아닌 타입 시스템으로 표현하여 오타와 중복을 컴파일 타임에 차단하고, 에러 발생 위치를 코드 자체로 즉시 파악할 수 있도록 하기 위해서입니다.

- **DomainErrorType**: `NotFound`, `InvalidState`, `InvalidTransition`, `DuplicateValue` 등 27종의 도메인 에러를 sealed record로 정의합니다. 27종은 실제 비즈니스 시나리오에서 반복 등장하는 도메인 에러 패턴을 정리한 결과입니다.
- **ApplicationErrorType**: `Unauthorized`, `Forbidden`, `Conflict` 등 애플리케이션 레이어 에러를 정의합니다.
- **AdapterErrorType**: `ExternalServiceFailure`, `DatabaseError` 등 어댑터 레이어 에러를 정의합니다.
- **에러 코드 형식**: `Domain.Order.InvalidTransition`, `Application.Auth.Unauthorized` 같은 `{Layer}.{Context}.{Name}` 형식의 구조화된 코드가 자동 생성되어, 로그에서 에러 코드만으로 레이어와 발생 Aggregate를 즉시 식별합니다.
- **팩토리**: `DomainError.For<T>()` 메서드가 제네릭 타입 `T`에서 Context 정보를 자동 추출하여 에러 코드를 생성하므로 수동 문자열 조합이 불필요합니다.

### 결과

- <span class="adr-good">Good</span>, because `DomainErrorType.NotFound`를 사용하면 `"Notfound"` 같은 오타가 컴파일 오류로 즉시 발견되어 런타임 매칭 실패가 원천 차단됩니다.
- <span class="adr-good">Good</span>, because sealed record 계층에 대한 `switch` 표현식에서 미처리 에러 타입이 컴파일 경고로 표시되어 모든 에러 케이스의 exhaustive 처리가 보장됩니다.
- <span class="adr-good">Good</span>, because 로그에 `Domain.Order.InvalidTransition`이 기록되면 "도메인 레이어의 Order Aggregate에서 상태 전이 실패"를 에러 코드만으로 즉시 파악할 수 있습니다.
- <span class="adr-good">Good</span>, because `DomainError.For<Order>()` 호출 시 제네릭 타입에서 Context(`Order`)를 자동 추출하므로 에러 코드 문자열을 수동으로 조합할 필요가 없습니다.
- <span class="adr-bad">Bad</span>, because 레이어별 sealed record 계층(DomainErrorType 27종 + ApplicationErrorType + AdapterErrorType)의 초기 설계와 분류 작업에 상당한 투자가 필요합니다.
- <span class="adr-bad">Bad</span>, because 새로운 도메인 에러 패턴이 등장하면 sealed record 계층에 새 타입을 추가하고, 기존 `switch` 표현식에 해당 케이스를 처리해야 합니다.

### 확인

- 모든 에러 타입이 해당 레이어의 sealed record 계층에 속하는지 아키텍처 규칙 테스트로 확인합니다.
- 에러 코드 형식이 `{Layer}.{Context}.{Name}` 패턴을 준수하는지 단위 테스트로 검증합니다.

## 옵션별 장단점

### 레이어별 sealed record 계층 + 에러 코드 자동 생성

- <span class="adr-good">Good</span>, because `DomainErrorType.NotFound`처럼 타입으로 에러를 표현하므로 오타, 대소문자 불일치가 컴파일 타임에 차단됩니다.
- <span class="adr-good">Good</span>, because sealed record에 대한 `switch` 표현식이 미처리 케이스를 컴파일 경고로 알려주어 에러 처리 누락을 방지합니다.
- <span class="adr-good">Good</span>, because `Domain.Order.InvalidTransition` 형식의 구조화된 에러 코드가 로그 검색, Grafana 대시보드 필터, API 응답에서 일관되게 사용됩니다.
- <span class="adr-good">Good</span>, because `IHasErrorCode` 인터페이스를 통해 도메인/애플리케이션/어댑터 에러가 동일한 형식(`{Layer}.{Context}.{Name}`)으로 통일되어 레이어를 초월한 에러 처리 파이프라인을 구성할 수 있습니다.
- <span class="adr-bad">Bad</span>, because 27종의 DomainErrorType + ApplicationErrorType + AdapterErrorType 계층을 초기에 설계하고, 새로운 에러 패턴 등장 시 계층을 확장해야 하는 유지보수 비용이 있습니다.

### enum 기반 에러 타입

- <span class="adr-good">Good</span>, because `DomainError.NotFound` 같은 enum 멤버로 오타를 방지할 수 있어 문자열보다 안전합니다.
- <span class="adr-bad">Bad</span>, because enum 멤버는 속성을 가질 수 없으므로 `InvalidTransition`에 `FromState`, `ToState` 같은 컨텍스트 정보를 함께 전달할 수 없어 별도 클래스가 추가로 필요합니다.
- <span class="adr-bad">Bad</span>, because `DomainError` enum과 `ApplicationError` enum을 분리해도 메서드 시그니처에서 `int`로 암묵 변환되어 레이어 간 타입 구분이 실질적으로 약합니다.
- <span class="adr-bad">Bad</span>, because 새로운 에러 유형 추가 시 기존 enum에 항목을 추가해야 하므로, enum을 참조하는 모든 `switch`문이 영향을 받아 Open-Closed Principle에 위배됩니다.

### 문자열 상수

- <span class="adr-good">Good</span>, because `const string NotFound = "NotFound";`로 정의가 가장 단순하며 별도 타입 설계가 불필요합니다.
- <span class="adr-bad">Bad</span>, because `ErrorCodes.NotFound`와 `"NotFound"` 리터럴이 혼용되면 상수를 쓰지 않은 곳의 오타가 컴파일 타임에 발견되지 않습니다.
- <span class="adr-bad">Bad</span>, because 문자열에 레이어/컨텍스트 정보를 포함하려면 `"Domain.Order.NotFound"` 같은 명명 규칙에 의존해야 하며, 규칙 위반을 강제할 수 없습니다.
- <span class="adr-bad">Bad</span>, because 에러 코드 문자열을 변경하면 IDE의 "Rename" 리팩토링이 동작하지 않아 전체 코드베이스를 문자열 검색으로 수동 수정해야 합니다.

### 예외 클래스 계층

- <span class="adr-good">Good</span>, because `OrderNotFoundException : DomainException` 같은 계층 구조가 .NET 개발자에게 익숙한 예외 처리 패턴과 일치합니다.
- <span class="adr-bad">Bad</span>, because `try-catch` 기반 제어 흐름은 `Fin<T>`의 `Map`/`Bind` 파이프라인과 근본적으로 양립할 수 없어, 코드베이스에 두 가지 에러 처리 패러다임이 혼재합니다.
- <span class="adr-bad">Bad</span>, because .NET 예외는 스택 트레이스 캡처 비용이 높아, 비즈니스 규칙 위반처럼 빈번히 발생하는 "예상된 실패"에 사용하면 불필요한 성능 저하가 발생합니다.
- <span class="adr-bad">Bad</span>, because ADR-0002에서 예외 대신 `Fin<T>`로 실패를 표현하기로 결정했으므로, 에러 타입을 다시 예외 클래스로 정의하면 기존 아키텍처 결정과 직접 상충합니다.

### 단일 ErrorType (레이어 구분 없음)

- <span class="adr-good">Good</span>, because 모든 에러가 하나의 `ErrorType` 계층에 속하여 구조가 단순하고 학습 비용이 낮습니다.
- <span class="adr-bad">Bad</span>, because `NotFound`가 "주문이 DB에 없음"(도메인)인지 "외부 API가 404 반환"(어댑터)인지 에러 타입만으로 구분할 수 없어, 모니터링에서 도메인 문제와 인프라 문제가 뒤섞입니다.
- <span class="adr-bad">Bad</span>, because 에러 처리 시 "도메인 에러면 400 반환, 어댑터 에러면 502 반환" 같은 레이어별 HTTP 상태 코드 매핑이 에러 타입 분기만으로는 불가능합니다.

## 관련 정보

- 관련 스펙: `spec/04-error-system`
- 관련 API: `DomainError.For<T>()` 팩토리
- 관련 ADR: [ADR-0002: 예외 대신 Fin 타입으로 실패 표현](./0002-use-fin-over-exceptions/)
