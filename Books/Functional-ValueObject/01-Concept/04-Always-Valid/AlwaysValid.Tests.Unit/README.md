# AlwaysValid.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `AlwaysValid` 프로젝트의 값 객체와 수학 연산 함수에 대한 단위 테스트를 포함합니다. Denominator 값 객체의 생성과 검증, 그리고 이를 활용한 안전한 나눗셈 연산을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트
#### `Create_ShouldReturnSuccessResult_WhenValueIsNotZero`
- **목적**: 유효한 값(0이 아닌 정수)으로 Denominator 생성 시 성공 결과 반환 확인
- **입력**: validValue = 5
- **결과**: 성공 결과와 함께 값 5를 가진 Denominator 객체

#### `Value_ShouldReturnCorrectValue_WhenDenominatorIsCreated`
- **목적**: 생성된 Denominator의 Value 속성이 올바른 값을 반환하는지 확인
- **입력**: expectedValue = 42
- **결과**: 42

#### `Divide_ShouldReturnCorrectResult_WhenUsingValidDenominator`
- **목적**: 유효한 Denominator 값 객체를 사용한 안전한 나눗셈 연산이 올바른 결과를 반환하는지 확인
- **입력**: numerator = 15, denominatorValue = 3
- **결과**: 5

### 예외/실패 케이스 테스트
#### `Create_ShouldReturnFailureResult_WhenValueIsZero`
- **목적**: 유효하지 않은 값(0)으로 Denominator 생성 시 실패 결과 반환 확인
- **입력**: invalidValue = 0
- **결과**: 실패 결과와 함께 "0은 허용되지 않습니다" 에러 메시지

### 순수성 테스트
#### `Create_ShouldBePureFunction_WhenDenominatorIsNotZero`
- **목적**: 유효한 같은 입력에 대해 항상 동일한 결과 반환 확인
- **입력**: denominator = 2
- **결과**: 여러 번 호출 시에도 동일한 성공 결과

#### `Create_ShouldBePureFunction_WhenDenominatorIstZero`
- **목적**: 유효하지 않은 같은 입력(0)에 대해 항상 동일한 결과 반환 확인
- **입력**: denominator = 0
- **결과**: 여러 번 호출 시에도 동일한 실패 결과와 에러 메시지

#### `Denominator_ShouldBeImmutable_WhenValueIsAccessed`
- **목적**: Denominator 값 객체의 불변성 유지 확인
- **입력**: originalValue = 10
- **결과**: 여러 번 접근해도 동일한 값 반환

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 7
- **성공**: 7
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 3개 테스트 케이스
- **예외/실패 케이스**: 1개 테스트 케이스
- **순수성 테스트**: 3개 테스트 케이스

### 학습 목표 달성도
✅ **핵심 개념 이해**: Denominator 값 객체의 생성과 검증 메커니즘 이해
✅ **문제점 인식**: 0 값 입력에 대한 적절한 실패 처리 인식
✅ **개선 방향 이해**: 항상 유효한 타입 패턴의 장점과 활용 방법 이해
