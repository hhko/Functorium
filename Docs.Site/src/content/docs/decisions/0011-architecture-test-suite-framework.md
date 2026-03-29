---
title: "ADR-0011: 아키텍처 테스트 Suite 프레임워크 직접 구현"
status: "accepted"
date: 2026-03-20
---

## 맥락과 문제

DDD 기반 프로젝트에서 아키텍처 규칙(sealed Value Object, private 생성자, 불변성, 레이어 의존성 방향)을 코드 리뷰에만 의존하면 규칙 위반이 누적된다. ArchUnitNET은 레이어 간 의존성 검증에는 강하지만, C# 타입 시스템 수준의 DDD 규칙(sealed 여부, 생성자 접근 제한자, 프로퍼티 불변성)을 표현하기에는 API가 부족하다.

DDD의 전술적 패턴을 컴파일 타임에 가깝게 강제할 수 있는 아키텍처 테스트 프레임워크가 필요하다.

## 검토한 옵션

- **옵션 1**: ArchUnitNET 단독 사용
- **옵션 2**: NetArchTest 사용
- **옵션 3**: ClassValidator/InterfaceValidator/MethodValidator 직접 구현 + Suite 상속 패턴
- **옵션 4**: 수동 코드 리뷰

## 결정

**옵션 3: Validator 직접 구현 + Suite 상속 패턴을 채택한다.**

`ClassValidator`, `InterfaceValidator`, `MethodValidator`를 직접 구현하여 C# 리플렉션 기반으로 DDD 규칙을 검증한다. 이를 `DomainArchitectureTestSuite` 등의 Suite 클래스로 조합하여 상속만으로 프로젝트별 아키텍처 테스트를 적용할 수 있게 한다.

주요 검증 규칙:
- **ImmutabilityRule**: Value Object와 Entity의 프로퍼티가 `init` 또는 `private set`인지 검증
- **SealedRule**: Value Object가 sealed인지 검증
- **PrivateConstructorRule**: Aggregate Root의 기본 생성자가 private인지 검증
- **LayerDependencyRule**: 레이어 간 의존성 방향이 올바른지 검증

xUnit과 디커플링하여 테스트 프레임워크에 독립적으로 동작하며, 3계층(Validator → Rule → Suite)에서 1계층(Suite가 Rule을 직접 포함)으로 단순화하였다.

### 결과

- **긍정적**: DDD 전술 패턴 위반을 CI에서 자동으로 잡아낸다. Suite 상속만으로 새 프로젝트에 즉시 적용할 수 있다. 커스텀 규칙 추가가 용이하다. xUnit 디커플링으로 다른 테스트 프레임워크에서도 사용 가능하다.
- **부정적**: 프레임워크 자체의 유지보수 비용이 발생한다. 리플렉션 기반이므로 C# 언어 변경(예: 새로운 접근 제한자) 시 업데이트가 필요하다.

### 확인

- DomainArchitectureTestSuite를 상속한 테스트 클래스가 sealed 위반, 불변성 위반을 정확히 감지하는지 확인한다.
- 의도적으로 규칙을 위반하는 테스트 코드를 작성하여 false negative가 없는지 검증한다.
- CI 파이프라인에서 아키텍처 테스트가 빌드 실패로 연결되는지 확인한다.

## 옵션별 장단점

### 옵션 1: ArchUnitNET 단독 사용

- **장점**: 성숙한 오픈소스 라이브러리로 레이어 의존성 검증에 강하다. Fluent API로 규칙을 선언적으로 작성할 수 있다. 커뮤니티 지원과 문서가 풍부하다.
- **단점**: DDD 전술 패턴(sealed VO, private 생성자, init-only 프로퍼티) 검증을 직접 표현할 수 없다. 커스텀 규칙 확장 API가 제한적이다. C# 타입 시스템 수준의 세밀한 검증이 어렵다.

### 옵션 2: NetArchTest 사용

- **장점**: .NET 전용으로 C# 프로젝트와 자연스럽게 통합된다. Fluent API가 직관적이다.
- **단점**: 커스텀 규칙 확장 포인트가 제한적이다. DDD 특화 규칙(불변성, CRTP 패턴 검증)을 추가하기 어렵다. 유지보수 활동이 활발하지 않다.

### 옵션 3: Validator 직접 구현 + Suite 상속 패턴

- **장점**: DDD 전술 패턴에 최적화된 규칙을 자유롭게 구현할 수 있다. Suite 상속으로 재사용성이 높다. xUnit 디커플링으로 테스트 프레임워크에 독립적이다. C# 리플렉션의 모든 기능을 활용하여 세밀한 검증이 가능하다. 1계층 구조로 단순하다.
- **단점**: 직접 구현 및 유지보수 비용이 발생한다. 레이어 의존성 검증은 ArchUnitNET보다 표현력이 떨어질 수 있다. 리플렉션 기반이므로 런타임 성능에 영향을 줄 수 있다(테스트 시에만 실행).

### 옵션 4: 수동 코드 리뷰

- **장점**: 도구 도입 비용이 없다. 유연하게 판단할 수 있다.
- **단점**: 사람의 실수로 규칙 위반이 누적된다. 리뷰어에 따라 기준이 달라진다. 프로젝트 규모가 커질수록 리뷰 비용이 기하급수적으로 증가한다. 자동화된 피드백 루프가 없다.

## 관련 정보

- 커밋: 7a073b9d (xUnit 디커플링, ImmutabilityRule), 5af2b12b (3계층에서 1계층 Suite로 단순화)
