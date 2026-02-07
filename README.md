# Functorium

배움은 설렘이다. 배움은 겸손이다. 배움은 이타심이다.

[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

> A functional domain is **`functor + dominium`**, seasoned with **`fun`**, designed to bridge **결정론적 규칙의 시대(the age of deterministic rules)와 확률론적 직관의 시대(the age of probabilistic intuition)**.
>
> - `Domain-Driven Design`: 객체 단위로 비즈니스 관심사를 캡슐화한다.
> - `Functional Architecture`: 레이어 단위로 비즈니스 관심사를 순수화한다.
> - `Microservices Architecture`: 서비스 단위로 비즈니스 관심사를 자율화한다.
>
> 그래서 우리는 유스케이스 단위를 최상위 설계 단위로 삼는다!

![](./Functorium.Architecture.png)

도메인 로직을 순수 함수로 표현하고 부수 효과를 아키텍처 경계로 밀어내어 **테스트 가능하고 예측 가능한 비즈니스 로직**을 작성할 수 있습니다. 이 프레임워크는 LanguageExt 5.x 기반의 도메인 중심 함수형 아키텍처와 OpenTelemetry를 통한 통합 관측성을 제공합니다.

### 핵심 원칙

| 원칙 | 설명 | Functorium 지원 |
|------|------|-----------------|
| **Domain First** | 도메인 모델이 아키텍처의 중심 | Value Object 계층, 불변 도메인 타입 |
| **Pure Core** | 비즈니스 로직을 순수 함수로 표현 | `Fin<T>` 반환 타입, 예외 없는 오류 처리 |
| **Impure Shell** | 부수 효과를 경계 레이어에서 처리 | Adapter Pipeline, ActivityContext 전파 |
| **Explicit Effects** | 모든 효과를 명시적으로 타입화 | `FinResponse<T>`, `FinT<IO, T>` 모나드 |

## Book
- [Architecture](./Docs/ArchitectureIs/README.md)
- [Automating Release Notes with Claude Code and .NET 10](./Books/Automating-ReleaseNotes-with-ClaudeCode-and-.NET10/README.md)
- [Automating Observability Code with SourceGenerator](./Books/Automating-ObservabilityCode-with-SourceGenerator/README.md)
- [Implementing Functional ValueObject](./Books/Implementing-Functional-ValueObject/README.md)

## Observability

Functorium은 OpenTelemetry 기반의 통합 관측성(Logging, Metrics, Tracing)을 제공합니다.
모든 관측성 필드는 OpenTelemetry 시맨틱 규칙과의 일관성을 위해 `snake_case + dot` 표기법을 사용합니다.

![](./Functorium.Observability.png)

- **레퍼런스**: [Observability Reference](./.claude/guides/observability-reference.md) — Field/Tag 구조, Meter/Instrument 사양, 메시지 템플릿
- **Logging 매뉴얼**: [Logging Guide](./.claude/guides/_observability-logging.md) — 구조화된 로깅 상세 가이드
- **Metrics 매뉴얼**: [Metrics Guide](./.claude/guides/_observability-metrics.md) — 메트릭 수집 및 분석 가이드
- **Tracing 매뉴얼**: [Tracing Guide](./.claude/guides/_observability-tracing.md) — 분산 추적 상세 가이드
