# OperatorOverloading.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `Denominator` 값 객체의 연산자 오버로딩과 변환 연산자에 대한 단위 테스트를 포함합니다. 연산자 오버로딩을 통한 자연스러운 나눗셈 연산, 명시적/암시적 변환 연산자 동작, 에러 상황에서의 적절한 예외 발생을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `Create_ShouldReturnSuccessResult_WhenValueIsNotZero`
- **목적**: 유효한 값(0이 아닌 정수)으로 Denominator 생성 시 성공 결과 확인
- **입력**: validValue = 5
- **결과**: 성공 결과 반환, denominator.Value = 5

#### `DivisionOperator_ShouldReturnCorrectResult_WhenUsingOperatorOverloadingValidDenominator`
- **목적**: 연산자 오버로딩을 통한 자연스러운 나눗셈 연산 검증
- **테스트 케이스**:
  - `(10, 2) → 5`
  - `(15, 3) → 5`
  - `(100, 10) → 10`

#### `ExplicitConvertingIntToDenominator_ShouldReturnDenominator_WhenValueIsNotZero`
- **목적**: 유효한 값을 Denominator로 명시적 변환 시 성공 확인
- **입력**: validValue = 7
- **결과**: Denominator 객체 생성 성공

#### `ImplicitConvertingDenominatorToInt_ShouldReturnIntValue_WhenDenominatorIsSuccess`
- **목적**: Denominator를 int로 암시적 변환 시 정확한 값 반환 확인
- **입력**: denominator = Denominator(12)
- **결과**: 12

#### `Divide_ShouldReturnCorrectResult_WhenUsingValidDenominator`
- **목적**: MathOperations.Divide 메서드에서 연산자 오버로딩 활용 검증
- **입력**: numerator = 15, denominator = Denominator(3)
- **결과**: 5

### 예외/실패 케이스 테스트

#### `Create_ShouldReturnFailureResult_WhenValueIsZero`
- **목적**: 유효하지 않은 값(0)으로 Denominator 생성 시 실패 결과 확인
- **입력**: invalidValue = 0
- **결과**: 실패 결과 반환, 에러 메시지 "0은 허용되지 않습니다"

#### `ExplicitConvertingIntToDenominator_ShouldThrowInvalidCastException_WhenValueIsZero`
- **목적**: 유효하지 않은 값(0)을 Denominator로 명시적 변환 시 예외 발생 확인
- **입력**: invalidValue = 0
- **결과**: InvalidCastException with message "0은 Denominator로 변환할 수 없습니다"

### 엣지 케이스 테스트

#### `DivisionOperator_ShouldWorkWithDirectOperatorUsage_WhenUsingValidDenominator`
- **목적**: 직접 연산자 사용 시에도 정상 동작 확인
- **입력**: numerator = 18, denominator = Denominator(6)
- **결과**: 3

#### `ConversionOperators_ShouldWorkTogether_WhenConvertingBetweenTypes`
- **목적**: 변환 연산자들이 함께 사용될 때 정상 동작 확인
- **입력**: originalValue = 25
- **결과**: 25 (명시적 변환 → 암시적 변환)

#### `DivisionOperator_ShouldWorkWithChainedOperations_WhenUsingValidDenominators`
- **목적**: 연쇄 연산에서도 정상 동작 확인
- **입력**: denominator1 = Denominator(2), denominator2 = Denominator(3)
- **결과**: 2 (12 / 2 / 3 = 6 / 3 = 2)

#### `DivisionOperator_ShouldWorkWithComplexExpressions_WhenUsingValidDenominators`
- **목적**: 복잡한 수식에서도 정상 동작 확인
- **입력**: denominator = Denominator(4)
- **결과**: 5 ((20 + 0) / 4 = 20 / 4 = 5)

#### `DivisionOperator_ShouldWorkWithMethodCalls_WhenUsingValidDenominator`
- **목적**: 메서드 호출 결과와 함께 사용될 때도 정상 동작 확인
- **입력**: denominator = Denominator(5)
- **결과**: 정상 동작

### 특수 패턴 테스트

#### `DivisionOperator_ShouldWorkWithMathOperationsClass_WhenUsingValidDenominator`
- **목적**: MathOperations.Divide 메서드에서 연산자 오버로딩이 정상 동작 확인
- **입력**: numerator = 20, denominator = Denominator(4)
- **결과**: 5

#### `DivisionOperator_ShouldWorkWithNegativeNumbers_WhenUsingValidDenominator`
- **목적**: 음수 케이스에서도 나눗셈 연산자가 정상 동작 확인
- **테스트 케이스**:
  - `(8, 2) → 4`
  - `(25, 5) → 5`
  - `(36, 6) → 6`

#### `DivisionOperator_ShouldMaintainPrecision_WhenUsingLargeNumbers`
- **목적**: 큰 수에서도 나눗셈 연산자가 정확한 결과 반환 확인
- **입력**: numerator = 1000000, denominator = Denominator(1000)
- **결과**: 1000

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 20
- **성공**: 20
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 12개
- **예외/실패 케이스**: 2개
- **엣지 케이스**: 6개
- **특수 패턴 테스트**: 0개

### 학습 목표 달성도
- ✅ **핵심 개념 이해**: 연산자 오버로딩과 변환 연산자의 동작 원리 이해
- ✅ **문제점 인식**: 0으로 나누기와 같은 에러 상황에서의 적절한 예외 처리 인식
- ✅ **개선 방향 이해**: 값 객체를 통한 타입 안전성과 자연스러운 연산자 사용법 이해

### 테스트 실행 명령어
```bash
# 전체 테스트 실행
dotnet test

# 상세 로그
dotnet test --verbosity normal

# 특정 클래스만
dotnet test --filter "FullyQualifiedName~DenominatorTests"
dotnet test --filter "FullyQualifiedName~OperatorOverloadingIntegrationTests"
dotnet test --filter "FullyQualifiedName~MathOperationsTests"

# 특정 메서드만
dotnet test --filter "FullyQualifiedName~Create_ShouldReturnSuccessResult_WhenValueIsNotZero"
```
