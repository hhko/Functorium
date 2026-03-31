---
title: "ADR-0022: 아키텍처 테스트 Suite 프레임워크 직접 구현"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

PR 리뷰에서 "이 Value Object에 sealed 빠졌습니다", "이 프로퍼티 `set`을 `init`으로 바꿔야 합니다" 같은 동일한 코멘트가 매주 반복되었다. 사람이 검사하는 방식으로는 규칙 위반이 누적될 수밖에 없었고, 리뷰어에 따라 기준이 달라지기도 했다. ArchUnitNET을 도입해 자동화를 시도했으나, 레이어 간 의존성 검증에는 강한 반면 "Value Object가 sealed인가", "Aggregate Root의 기본 생성자가 private인가", "프로퍼티가 init-only인가" 같은 C# 타입 시스템 수준의 DDD 전술 규칙을 표현하는 API가 없었다. 커스텀 규칙을 만들려 해도 확장 포인트가 제한적이어서, 결국 ArchUnitNET 위에 억지로 끼워 맞추는 것보다 직접 만드는 편이 낫겠다는 판단에 이르렀다.

DDD 전술적 패턴(sealed, private 생성자, 불변성)을 CI에서 자동으로 강제할 수 있는 아키텍처 테스트 프레임워크가 필요했다.

## 검토한 옵션

- **옵션 1**: ArchUnitNET 단독 사용
- **옵션 2**: NetArchTest 사용
- **옵션 3**: ClassValidator/InterfaceValidator/MethodValidator 직접 구현 + Suite 상속 패턴
- **옵션 4**: 수동 코드 리뷰

## 결정

**옵션 3: Validator 직접 구현 + Suite 상속 패턴을 채택한다.**

ArchUnitNET과 NetArchTest를 실제로 시도한 결과, 레이어 의존성은 잘 검증하지만 "이 Value Object가 sealed인가?"라는 단순한 질문조차 표현할 수 없었다. C# 리플렉션이 제공하는 `IsSealed`, `GetConstructors()`, 프로퍼티의 `SetMethod.IsInitOnly` 같은 세밀한 타입 정보에 직접 접근해야만 DDD 전술 규칙을 정확히 검증할 수 있다.

`ClassValidator`, `InterfaceValidator`, `MethodValidator`를 직접 구현하고, 이를 `DomainArchitectureTestSuite` 등의 Suite 클래스로 조합하여 상속 한 줄로 프로젝트별 아키텍처 테스트를 적용할 수 있게 한다.

주요 검증 규칙:
- **ImmutabilityRule**: Value Object와 Entity의 프로퍼티가 `init` 또는 `private set`인지 검증
- **SealedRule**: Value Object가 sealed인지 검증
- **PrivateConstructorRule**: Aggregate Root의 기본 생성자가 private인지 검증
- **LayerDependencyRule**: 레이어 간 의존성 방향이 올바른지 검증

xUnit과 디커플링하여 테스트 프레임워크에 독립적으로 동작하며, 초기 3계층(Validator -> Rule -> Suite)에서 1계층(Suite가 Rule을 직접 포함)으로 단순화하였다.

### 결과

- **긍정적**: PR에서 반복되던 "sealed 빠짐", "init으로 변경 필요" 같은 리뷰 코멘트가 CI 단계에서 자동으로 잡혀 사라졌다. 새 프로젝트에 `DomainArchitectureTestSuite`를 상속하는 클래스 하나만 추가하면 모든 DDD 규칙이 즉시 적용된다. xUnit 디커플링 덕분에 테스트 프레임워크를 교체해도 규칙 자체는 재사용 가능하다.
- **부정적**: 프레임워크 자체의 유지보수 부담이 생긴다. 특히 C# 언어 변경(예: 새로운 접근 제한자 도입)이 있으면 리플렉션 기반 검증 로직을 함께 업데이트해야 한다.

### 확인

- DomainArchitectureTestSuite를 상속한 테스트 클래스가 sealed 위반, 불변성 위반을 정확히 감지하는지 확인한다.
- 의도적으로 규칙을 위반하는 테스트 코드를 작성하여 false negative가 없는지 검증한다.
- CI 파이프라인에서 아키텍처 테스트가 빌드 실패로 연결되는지 확인한다.

## 옵션별 장단점

### 옵션 1: ArchUnitNET 단독 사용

- **장점**: 성숙한 오픈소스 라이브러리로, `Types().That().ResideInNamespace("Domain").ShouldNot().DependOn("Infrastructure")` 같은 Fluent API로 레이어 의존성을 선언적으로 검증할 수 있다. 커뮤니티 지원과 문서가 풍부하다.
- **단점**: "Value Object가 sealed인가?"를 표현하는 API가 없다. `IsSealed`, 생성자 접근 제한자, `init`-only 프로퍼티 같은 C# 타입 시스템 수준의 검증이 불가능하다. 커스텀 규칙 확장 API가 제한적이어서 DDD 전술 패턴을 억지로 끼워 맞춰야 한다.

### 옵션 2: NetArchTest 사용

- **장점**: .NET 전용이라 C# 프로젝트와 자연스럽게 통합된다. `Types.InAssembly().That().AreClasses().Should().BeSealed()` 수준의 Fluent API를 제공한다.
- **단점**: sealed 검증은 가능하나 "init-only 프로퍼티 검증", "CRTP 패턴으로 상속된 base class 확인" 같은 DDD 특화 규칙을 추가할 확장 포인트가 부족하다. 라이브러리 유지보수가 활발하지 않아 최신 C# 기능 대응이 느리다.

### 옵션 3: Validator 직접 구현 + Suite 상속 패턴

- **장점**: `Type.IsSealed`, `PropertyInfo.SetMethod.IsInitOnly`, `ConstructorInfo.IsPrivate` 등 C# 리플렉션의 모든 기능을 활용하여 DDD 전술 규칙을 정확히 표현할 수 있다. `DomainArchitectureTestSuite`를 상속하는 한 줄로 새 프로젝트에 즉시 적용 가능하다. xUnit에 의존하지 않아 테스트 프레임워크 교체에도 영향 없다. 1계층 구조로 내부가 단순하다.
- **단점**: 직접 구현하므로 유지보수 비용이 발생한다. 레이어 의존성 검증의 Fluent 표현력은 ArchUnitNET에 미치지 못할 수 있다. 리플렉션 기반이므로 테스트 실행 시 약간의 성능 비용이 있다(CI에서만 실행하므로 실질적 영향은 미미).

### 옵션 4: 수동 코드 리뷰

- **장점**: 도구 도입 비용이 제로다. 상황에 따라 유연하게 판단할 수 있다.
- **단점**: "sealed 빠졌습니다" 같은 반복 코멘트가 리뷰어의 피로를 유발하고, 리뷰어에 따라 기준이 달라진다. 프로젝트 규모가 커질수록 검사해야 할 타입 수가 늘어나 리뷰 비용이 기하급수적으로 증가하며, 누락은 CI가 아닌 프로덕션에서 발견된다.

## 관련 정보

- 커밋: 7a073b9d (xUnit 디커플링, ImmutabilityRule), 5af2b12b (3계층에서 1계층 Suite로 단순화)
