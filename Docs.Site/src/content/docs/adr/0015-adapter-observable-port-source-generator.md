---
title: "ADR-0015: Observable Port 소스 제너레이터"
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

- <span class="adr-good">Good</span>, because 새 포트를 추가할 때 `[GenerateObservablePort]` 어트리뷰트 한 줄이면 Tracing Span, 구조화 로그, Metrics 카운터/히스토그램 코드가 자동 생성되어 관측성 적용 누락이 불가능합니다.
- <span class="adr-good">Good</span>, because 컴파일 타임에 C# 소스 코드로 생성되므로 런타임 리플렉션 비용이 제로이고, Native AOT 환경과도 호환됩니다.
- <span class="adr-good">Good</span>, because 생성된 `Observable{PortName}.g.cs` 파일을 IDE에서 직접 열어 디버깅하고 코드 리뷰할 수 있습니다.
- <span class="adr-bad">Bad</span>, because Incremental Generator API, Roslyn 심볼 분석, 소스 텍스트 에미팅 등 소스 제너레이터 고유의 전문 지식이 필요하여 유지보수 가능 인원이 제한됩니다.
- <span class="adr-bad">Bad</span>, because 일부 IDE에서 생성된 코드의 IntelliSense나 Go to Definition이 즉시 반영되지 않아, 빌드 후에야 네비게이션이 가능한 경우가 있습니다.

### 확인

- 포트 인터페이스에 `[GenerateObservablePort]` 어트리뷰트가 적용되어 있는지 확인합니다.
- 빌드 시 `Observable{PortName}` 클래스가 생성되는지 확인합니다.
- 생성된 데코레이터가 Tracing Span, 구조화 로그, Metrics 카운터/히스토그램을 올바르게 기록하는지 스냅샷 테스트로 검증합니다.

## 옵션별 장단점

### [GenerateObservablePort] 소스 제너레이터

- <span class="adr-good">Good</span>, because 컴파일 타임에 순수 C# 코드를 생성하므로 런타임 오버헤드가 제로이고, 호출 경로에 리플렉션이 개입하지 않습니다.
- <span class="adr-good">Good</span>, because 포트 인터페이스의 메서드 시그니처를 변경하면 다음 빌드에서 데코레이터가 자동 재생성되어 수동 동기화가 불필요합니다.
- <span class="adr-good">Good</span>, because 생성된 `.g.cs` 파일이 프로젝트에 포함되어 디버거 스텝인, 코드 리뷰, 스냅샷 테스트가 모두 가능합니다.
- <span class="adr-bad">Bad</span>, because Roslyn의 Incremental Generator API와 심볼 모델을 이해해야 하므로 제너레이터 자체의 개발 진입 장벽이 높습니다.
- <span class="adr-bad">Bad</span>, because 제너레이터에 버그가 있으면 빌드 오류 메시지가 생성된 코드를 가리키므로 원인 추적에 시간이 소요됩니다.

### 런타임 리플렉션 프록시 (DispatchProxy)

- <span class="adr-good">Good</span>, because `DispatchProxy.Create<TInterface, TProxy>()` 한 줄로 프록시를 생성할 수 있어 초기 구현이 간단합니다.
- <span class="adr-bad">Bad</span>, because 포트 메서드가 호출될 때마다 리플렉션으로 `MethodInfo`를 조회하고 파라미터를 boxing하여 성능 오버헤드가 누적됩니다.
- <span class="adr-bad">Bad</span>, because Native AOT 환경에서 런타임 타입 생성이 제한되어 동작하지 않을 수 있습니다.
- <span class="adr-bad">Bad</span>, because 프록시 코드가 소스에 존재하지 않아 디버거로 스텝인할 수 없고, Tracing/Logging/Metrics가 올바르게 기록되는지 스냅샷 테스트로 검증하기 어렵습니다.

### Decorator 수동 작성

- <span class="adr-good">Good</span>, because 소스 제너레이터나 리플렉션 없이 순수 C# 코드로 작성하므로 누구나 이해하고 수정할 수 있습니다.
- <span class="adr-bad">Bad</span>, because 10개 포트 x 3~5개 메서드 = 30~50개의 래핑 메서드를 수동으로 작성하고 유지해야 하며, 포트가 늘어날수록 코드량이 선형 증가합니다.
- <span class="adr-bad">Bad</span>, because 포트 인터페이스에 파라미터를 추가하면 데코레이터도 수정해야 하는데, 컴파일러가 이를 강제하지 않으면 동기화 누락이 조용히 발생합니다.
- <span class="adr-bad">Bad</span>, because 새 포트 추가 시 데코레이터 작성을 잊으면 해당 포트의 관측성이 통째로 빠지고, 운영 환경에서야 Tracing 누락으로 발견됩니다.

### AOP 프레임워크 (Castle.DynamicProxy 등)

- <span class="adr-good">Good</span>, because Interceptor 하나로 모든 포트에 공통 Aspect를 일괄 적용할 수 있어 초기 설정이 간편합니다.
- <span class="adr-bad">Bad</span>, because 런타임에 IL을 생성하여 프록시를 만들므로 앱 시작 시 초기화 비용이 발생하고, 호출 경로가 불투명해집니다.
- <span class="adr-bad">Bad</span>, because Castle.DynamicProxy 등 외부 라이브러리에 대한 추가 의존이 생기며, 해당 라이브러리의 업데이트 주기에 영향을 받습니다.
- <span class="adr-bad">Bad</span>, because Native AOT 환경에서 런타임 IL 생성이 차단되어 동작하지 않으며, Functorium의 AOT 호환성 목표와 충돌합니다.

## 관련 정보

- 관련 커밋: `a5027a78` feat(observability): ObservablePortGenerator 개선 + 프레임워크 필드 네이밍 통일
- 관련 커밋: `81233196` feat(source-generator): LogEnricher 소스 제너레이터 구현
- 관련 문서: `Docs.Site/src/content/docs/tutorials/sourcegen-observability/`
