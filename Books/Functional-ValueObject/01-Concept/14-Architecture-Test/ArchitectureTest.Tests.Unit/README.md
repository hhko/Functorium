# ArchitectureTest.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `ArchUnitNET`을 사용한 값 객체 아키텍처 규칙 검증에 대한 단위 테스트를 포함합니다. 값 객체의 설계 원칙과 구현 규칙을 자동으로 검증하여 코드 품질과 일관성을 보장합니다.

## 테스트 시나리오

### 아키텍처 규칙 테스트

#### `ValueObject_ShouldSatisfy_Rules`
- **목적**: IValueObject 인터페이스를 구현하는 모든 값 객체가 아키텍처 규칙을 준수하는지 검증
- **검증 대상**: 
  - IValueObject 인터페이스를 구현하는 모든 클래스
  - abstract 클래스는 제외 (ValueObject, `SimpleValueObject<T>` 등)
- **검증 규칙**:
  - **클래스 구조**: public sealed 클래스, 모든 생성자는 private
  - **불변성**: 모든 속성이 readonly로 선언되어야 함
  - **필수 메서드**: Create, CreateFromValidated, Validate 메서드의 시그니처 검증
  - **동등성**: `IEquatable<T>` 인터페이스 구현
  - **DomainErrors**: 중첩 클래스 구조 및 메서드 규칙 검증

#### 아키텍처 규칙 상세 검증

##### 값 객체 클래스 규칙
- **클래스 접근성**: `RequirePublic()` - 모든 값 객체는 public 클래스
- **클래스 봉인**: `RequireSealed()` - 상속을 방지하기 위해 sealed 클래스
- **생성자 제한**: `RequireAllPrivateConstructors()` - 외부에서 직접 생성 방지
- **불변성 보장**: `RequireImmutable()` - 모든 속성이 readonly로 선언
- **동등성 구현**: `RequireImplements(typeof(IEquatable<>))` - 값 기반 동등성 비교 지원

##### 필수 메서드 규칙
- **Create 메서드**: `public static Fin<T>` 시그니처
- **CreateFromValidated 메서드**: `internal static T` 시그니처  
- **Validate 메서드**: `public static Validation<Error, T>` 시그니처

##### DomainErrors 중첩 클래스 규칙
- **클래스 구조**: internal sealed 클래스
- **메서드 규칙**: public static Error 반환 타입

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 1
- **성공**: 1
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **아키텍처 규칙 테스트**: 1개
  - 값 객체 클래스 구조 검증
  - 필수 메서드 시그니처 검증
  - 불변성 보장 검증
  - DomainErrors 중첩 클래스 검증

### 학습 목표 달성도
- ✅ **아키텍처 테스트 이해**: ArchUnitNET을 사용한 아키텍처 규칙 검증 방법 학습
- ✅ **값 객체 설계 원칙**: 값 객체의 핵심 설계 원칙과 구현 규칙 이해
- ✅ **자동화된 품질 보장**: 컴파일 타임에 강제할 수 없는 설계 규칙을 런타임에 자동 검증
- ✅ **일관성 보장**: 모든 값 객체가 동일한 설계 원칙을 따르도록 강제
- ✅ **유지보수성 향상**: 아키텍처 규칙 위반을 조기에 발견하여 코드 품질 유지
