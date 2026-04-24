---
title: "API 사양 레퍼런스"
---

Functorium 프레임워크의 공개 타입, 인터페이스, 속성을 정의하는 **API 사양 문서**입니다. 설계 의도와 실습은 [프레임워크 가이드](../guides/)를, 여기서는 "무엇이 정확히 제공되는가"를 확인하십시오.

## 프로젝트 구조

| NuGet 패키지 | 네임스페이스 루트 | 역할 |
|-------------|-----------------|------|
| **Functorium** | `Functorium.Domains.*`, `Functorium.Applications.*`, `Functorium.Abstractions.*` | 도메인/애플리케이션 핵심 타입 |
| **Functorium.Adapters** | `Functorium.Adapters.*` | 인프라 어댑터, Pipeline, DI 등록 |
| **Functorium.SourceGenerators** | `Functorium.SourceGenerators.*` | 컴파일 타임 코드 생성기 |
| **Functorium.Testing** | `Functorium.Testing.*` | 테스트 유틸리티, 아키텍처 규칙 |

## 사양 목록

### 도메인 핵심

| 문서 | 설명 |
|------|------|
| [엔티티와 애그리거트](./01-entity-aggregate) | `Entity<TId>`, `AggregateRoot<TId>`, EntityId, 믹스인 인터페이스 |
| [값 객체](./02-value-object) | `ValueObject`, `SimpleValueObject<T>`, Union 타입, 동등성/비교 |
| [에러 시스템](./04-error-system) | `DomainErrorKind`, `ApplicationErrorKind`, `AdapterErrorKind`, 팩토리 API |

### 애플리케이션 계층

| 문서 | 설명 |
|------|------|
| [검증 시스템](./03-validation) | `TypedValidation`, `ContextualValidation`, FluentValidation 통합 |
| [유스케이스 CQRS](./05-usecase-cqrs) | `FinResponse<T>`, CQRS 인터페이스, LINQ 확장, 캐싱/영속성 계약 |
| [도메인 이벤트](./09-domain-events) | `DomainEvent`, Publisher/Collector, Handler, Ctx Enricher |

### 어댑터/인프라

| 문서 | 설명 |
|------|------|
| [포트와 어댑터](./06-port-adapter) | `IRepository`, `IQueryPort`, Specification, DI 등록, 구현 베이스 |
| [파이프라인](./07-pipeline) | 8개 Pipeline 동작, 커스텀 확장, `PipelineConfigurator`, OpenTelemetry 설정 |

### 횡단 관심사

| 문서 | 설명 |
|------|------|
| [관측 가능성](./08-observability) | Field/Tag 사양, Meter 정의, 메시지 템플릿, Pipeline 순서 |
| [소스 생성기](./10-source-generators) | 5개 소스 생성기 사양 (EntityId, ObservablePort, CtxEnricher, UnionType) |
| [테스트 라이브러리](./11-testing) | `FinTFactory`, 호스트 Fixture, 아키텍처 규칙, 에러 어설션 |
