---
title: "ADR-0015: Union Value Object 기반 상태 머신"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

주문 상태에 `PartiallyShipped`를 추가한 후, 코드베이스 전체의 `switch(orderStatus)` 20곳 중 3곳에서 새 상태에 대한 분기를 누락했다. 컴파일러는 `default` 분기가 있으므로 경고조차 내지 않았고, 부분 배송된 주문이 `default` 분기를 타면서 "알 수 없는 상태"로 처리되는 버그가 프로덕션까지 올라갔다. 또한 `enum` 기반에서는 Shipped에서 Draft로의 역전이 같은 비정상 상태 전이를 런타임 `if` 검사로만 막을 수 있어, 검사를 빼먹으면 도메인 불변식이 깨졌다. 상태별 부가 데이터(예: Shipped 상태에 추적번호 첨부)를 표현할 방법도 없었다.

새 상태 추가 시 모든 처리 지점에서 컴파일 에러를 발생시키고, 허용된 전이만 타입 수준에서 표현하여 잘못된 전이를 원천 차단할 수 있는 상태 머신 패턴이 필요했다.

## 검토한 옵션

- **옵션 1**: `enum` + `switch` 문
- **옵션 2**: SmartEnum 라이브러리
- **옵션 3**: `UnionValueObject<TSelf>` + `[UnionType]` Source Generator
- **옵션 4**: OneOf 라이브러리

## 결정

**옵션 3: `UnionValueObject<TSelf>` + `[UnionType]` Source Generator를 채택한다.**

`enum` + `switch`의 근본 문제는 "새 상태를 추가해도 컴파일러가 침묵한다"는 점이다. Source Generator가 생성하는 exhaustive `Match`/`Switch`는 이 문제를 원천 해결한다.

각 상태를 `UnionValueObject<TSelf>`를 상속하는 sealed record로 정의하고, `[UnionType]` 어트리뷰트를 부착하면 Source Generator가 `Match`/`Switch` 메서드를 자동 생성한다.

- **Match**: 모든 상태에 대한 함수를 인자로 받아 결과를 반환. 하나라도 빠지면 즉시 컴파일 에러.
- **Switch**: Match와 동일하나 반환값 없이 부수 효과만 수행.
- **상태 전이**: 각 상태 타입에 `TransitionTo()` 메서드를 정의하여 허용된 전이만 타입 수준에서 표현. Shipped에서 Draft로의 역전이 같은 비정상 전이는 `DomainErrorType.InvalidTransition`을 반환하며, 허용 목록에 없는 전이는 메서드 자체가 존재하지 않는다.

Value Object 기반이므로 불변성이 보장되고, 상태별 부가 데이터(예: Shipped 상태에 추적번호)를 타입 안전하게 첨부할 수 있으며, LINQ 등 컬렉션 연산과 자연스럽게 결합된다.

### 결과

- **긍정적**: `PartiallyShipped` 같은 새 상태를 추가하면, 코드베이스의 모든 Match/Switch 호출부에서 즉시 컴파일 에러가 발생하여 누락 지점을 빠짐없이 찾아준다. Shipped 상태에 추적번호를, Cancelled 상태에 취소 사유를 타입 안전하게 첨부할 수 있어 상태별 데이터 모델링이 명확해졌다. 상태 전이 규칙이 타입에 인코딩되어 별도 문서 없이도 코드가 곧 상태 다이어그램이다. Source Generator가 빌드 시점에 코드를 생성하므로 런타임 오버헤드가 없다.
- **부정적**: Source Generator의 빌드 의존성이 추가되며, IDE의 자동완성과 리팩토링 지원이 Source Generator 통합 품질에 의존한다. 함수형 Union 타입 패턴에 익숙하지 않은 C# 개발자에게 학습 곡선이 있다.

### 확인

- 새 상태를 추가한 후 기존 Match/Switch 호출부에서 컴파일 에러가 발생하는지 확인한다.
- 허용되지 않은 상태 전이가 `InvalidTransition` 에러를 반환하는지 테스트한다.
- Source Generator가 생성한 코드가 올바른 exhaustive check를 포함하는지 검증한다.

## 옵션별 장단점

### 옵션 1: enum + switch 문

- **장점**: C# 개발자에게 가장 친숙하다. 별도 라이브러리가 불필요하다. 직렬화/역직렬화가 단순하다.
- **단점**: 새 상태 추가 시 `switch`의 누락된 분기를 컴파일러가 에러로 잡지 않는다(경고만 발생하며, 대부분 `default` 분기로 무시됨). 상태 전이 규칙을 타입 수준에서 표현할 수 없다. 상태별 데이터(예: 배송 시 추적번호)를 첨부할 수 없다.

### 옵션 2: SmartEnum 라이브러리

- **장점**: enum보다 풍부한 동작을 정의할 수 있다. 상태별 메서드를 오버라이드하여 다형성을 활용한다.
- **단점**: 상태 전이 규칙을 컴파일 타임에 강제할 수 없다. exhaustive check가 불가능하다. 새 상태 추가 시 누락 검증이 없다. 불변성이 기본 보장되지 않는다.

### 옵션 3: UnionValueObject + [UnionType] Source Generator

- **장점**: exhaustive Match/Switch로 모든 상태를 컴파일 타임에 처리하도록 강제한다. 상태별 데이터를 타입 안전하게 첨부할 수 있다. Value Object의 불변성, 동등성이 기본 제공된다. Source Generator로 런타임 오버헤드가 없다. LINQ와 자연스럽게 결합된다.
- **단점**: Source Generator 빌드 의존성이 추가된다. 함수형 Union 타입에 대한 학습 곡선이 있다. 직렬화 시 커스텀 컨버터가 필요할 수 있다.

### 옵션 4: OneOf 라이브러리

- **장점**: Union 타입을 가볍게 사용할 수 있다. NuGet에서 바로 사용 가능하다.
- **단점**: LINQ와 결합이 자연스럽지 않다. Value Object 의미론(불변성, 동등성)이 내장되지 않는다. 상태 전이 규칙을 표현하는 메커니즘이 없다. Match 호출 시 위치 기반 인자라 가독성이 떨어진다.

## 관련 정보

- 커밋: 5c347e54, 3584b1db
