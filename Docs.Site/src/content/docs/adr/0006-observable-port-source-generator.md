---
title: "ADR-0006: Observable Port 소스 제너레이터"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

Functorium은 모든 포트(Port) 호출에 Tracing Span 생성, 구조화 로그 기록, Metrics 카운터/히스토그램 수집을 적용합니다. 이를 위해 포트 인터페이스마다 Observable 데코레이터를 작성해야 하는데, 문제는 그 작업이 완전한 반복 노동이라는 점입니다.

10개 포트에 각각 3~5개 메서드가 있다면 30~50개의 래핑 메서드를 수동으로 작성해야 합니다. 더 심각한 것은 유지보수입니다. `IPaymentPort.ChargeAsync`에 `CancellationToken` 파라미터를 추가하면, `ObservablePaymentPort`의 래핑 메서드도 동기화해야 합니다. 이 동기화를 놓치면 관측성이 조용히 깨지고, 운영 환경에서 해당 포트의 Tracing Span이 누락되어야 비로소 발견됩니다.

## 검토한 옵션

1. [GenerateObservablePort] 소스 제너레이터
2. 런타임 리플렉션 프록시 (DispatchProxy)
3. Decorator 수동 작성
4. AOP 프레임워크 (Castle.DynamicProxy 등)

## 결정

**선택한 옵션: "[GenerateObservablePort] 소스 제너레이터"**. 포트 인터페이스에 어트리뷰트 하나만 붙이면 컴파일 시점에 Tracing/Logging/Metrics 래퍼가 자동 생성됩니다. 포트 시그니처가 변경되면 다음 빌드에서 데코레이터도 자동 재생성되어 동기화 누락이 원천적으로 불가능하며, 런타임 리플렉션 비용도 제로입니다.

### 결과

- Good, because 포트 인터페이스에 `[GenerateObservablePort]`만 붙이면 Tracing/Logging/Metrics 래퍼가 자동 생성됩니다.
- Good, because 컴파일 타임 생성이므로 런타임 리플렉션 비용이 없습니다.
- Good, because 생성된 코드가 소스에 보이므로 디버깅과 코드 리뷰가 가능합니다.
- Bad, because 소스 제너레이터 개발 및 유지보수에 전문 지식이 필요합니다.
- Bad, because IDE 지원(IntelliSense, 네비게이션)이 런타임 코드보다 제한적일 수 있습니다.

### 확인

- 포트 인터페이스에 `[GenerateObservablePort]` 어트리뷰트가 적용되어 있는지 확인합니다.
- 빌드 시 `Observable{PortName}` 클래스가 생성되는지 확인합니다.
- 생성된 데코레이터가 Tracing Span, 구조화 로그, Metrics 카운터/히스토그램을 올바르게 기록하는지 스냅샷 테스트로 검증합니다.

## 옵션별 장단점

### [GenerateObservablePort] 소스 제너레이터

- Good, because 컴파일 타임 생성으로 런타임 오버헤드가 제로입니다.
- Good, because 포트 인터페이스 변경 시 자동으로 동기화됩니다.
- Good, because 생성된 코드를 직접 확인하고 디버깅할 수 있습니다.
- Bad, because 소스 제너레이터의 Incremental Generator API 학습이 필요합니다.
- Bad, because 제너레이터 버그 시 빌드 실패가 발생하며 원인 추적이 어려울 수 있습니다.

### 런타임 리플렉션 프록시 (DispatchProxy)

- Good, because 구현이 비교적 간단합니다.
- Bad, because 런타임 리플렉션으로 인해 호출마다 성능 오버헤드가 발생합니다.
- Bad, because AOT 컴파일 환경에서 동작하지 않을 수 있습니다.
- Bad, because 생성된 프록시가 소스에 보이지 않아 디버깅이 어렵습니다.

### Decorator 수동 작성

- Good, because 별도 도구 없이 직관적으로 구현 가능합니다.
- Bad, because 포트 수에 비례하여 보일러플레이트가 증가합니다.
- Bad, because 포트 인터페이스 변경 시 데코레이터 동기화를 수동으로 해야 합니다.
- Bad, because 관측성 적용 누락 위험이 높습니다.

### AOP 프레임워크 (Castle.DynamicProxy 등)

- Good, because 런타임에 유연하게 Aspect를 적용할 수 있습니다.
- Bad, because 런타임 프록시 생성 비용과 복잡도가 높습니다.
- Bad, because 외부 라이브러리 의존이 추가됩니다.
- Bad, because AOT 환경과 호환되지 않을 수 있습니다.

## 관련 정보

- 관련 커밋: `a5027a78` feat(observability): ObservablePortGenerator 개선 + 프레임워크 필드 네이밍 통일
- 관련 커밋: `81233196` feat(source-generator): LogEnricher 소스 제너레이터 구현
- 관련 문서: `Docs.Site/src/content/docs/tutorials/sourcegen-observability/`
