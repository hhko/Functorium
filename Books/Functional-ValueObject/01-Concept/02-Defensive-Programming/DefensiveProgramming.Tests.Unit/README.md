# DefensiveProgramming.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `DefensiveProgramming` 프로젝트의 `MathOperations` 클래스에 대한 단위 테스트를 포함합니다. 방어적 프로그래밍의 두 가지 구현 방법인 사전 검증을 통한 정의된 예외 처리와 TryDivide 패턴을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `Divide_ShouldReturnCorrectResult_WhenDenominatorIsNotZero`
- **목적**: 분모가 0이 아닌 경우 정상적인 나눗셈 결과 확인
- **입력**: numerator = 10, denominator = 2
- **결과**: 5

### 예외/실패 케이스 테스트

#### `Divide_ShouldThrowArgumentException_WhenDenominatorIsZero`
- **목적**: 분모가 0인 경우 ArgumentException 발생 확인
- **입력**: numerator = 10, denominator = 0
- **결과**: ArgumentException with message "0으로 나눌 수 없습니다" and paramName "denominator"

### TryDivide 패턴 테스트

#### `TryDivide_ShouldReturnTrueAndCorrectResult_ForValidDenominators`
- **목적**: 유효한 분모일 때 true를 반환하고 정확한 결과를 out 매개변수에 설정하는지 확인
- **입력**: numerator = 10, denominator = 2
- **결과**: success = true, result = 5

#### `TryDivide_ShouldReturnFalseAndDefaultResult_WhenDenominatorIsZero`
- **목적**: 분모가 0일 때 false를 반환하고 out 매개변수는 기본값인지 확인
- **입력**: numerator = 10, denominator = 0
- **결과**: success = false, result = 0 (default(int))

#### `TryDivide_ShouldNotThrowException_WhenDenominatorIsZero`
- **목적**: TryDivide는 예외를 발생시키지 않아야 함을 확인
- **입력**: numerator = 10, denominator = 0
- **결과**: 예외 발생 없음

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 5
- **성공**: 5
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 1개 테스트 케이스
- **예외/실패 케이스**: 1개 테스트 케이스
- **TryDivide 패턴 테스트**: 3개 테스트 케이스

### 학습 목표 달성도
- ✅ **방어적 프로그래밍의 두 가지 구현 방법 이해**: 사전 검증을 통한 정의된 예외 처리와 TryDivide 패턴 모두 테스트 완료
- ✅ **예외 기반 vs Try 패턴 방식의 차이점 이해**: 각각의 동작 방식과 장단점을 테스트를 통해 확인
- ✅ **Try 패턴의 장점과 한계점 습득**: 예외 발생 없음, out 매개변수를 통한 부작용 존재 등을 테스트로 검증
- ✅ **실무 표준 패턴과 다음 단계 연결 이해**: TryDivide 패턴이 .NET Framework 표준 패턴임을 확인하고, 여전히 부작용이 존재함을 인식


모든 테스트가 성공적으로 통과하여 방어적 프로그래밍의 두 가지 구현 방법이 올바르게 동작함을 확인했습니다.
