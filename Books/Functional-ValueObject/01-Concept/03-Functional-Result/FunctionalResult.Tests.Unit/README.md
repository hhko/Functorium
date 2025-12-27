# FunctionalResult.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `FunctionalResult` 프로젝트의 `MathOperations.Divide` 함수에 대한 단위 테스트를 포함합니다. 함수형 결과 타입의 장점, `Fin<T>` 타입의 활용법, 순수 함수의 중요성을 모두 테스트합니다.

## 테스트 시나리오

### 테스트 케이스 분류
- **정상 케이스 테스트**: 기본 동작 테스트
- **실패 케이스 테스트**: 오류 상황 테스트
- **순수성 테스트**: 함수의 순수성 검증

### 개별 테스트 메서드 설명

#### `Divide_ShouldReturnSuccessResult_WhenDenominatorIsNotZero`
- **목적**: 분모가 0이 아닌 경우 `Fin<int>.Succ(결과)` 반환 확인
- **입력**: numerator = 10, denominator = 2
- **결과**: `Fin<int>.Succ(5)` 반환, IsSucc = true

#### `Divide_ShouldReturnFailureResult_WhenDenominatorIsZero`
- **목적**: 분모가 0인 경우 `Fin<int>.Fail(Error)` 반환 확인
- **입력**: numerator = 10, denominator = 0
- **결과**: `Fin<int>.Fail(Error)` 반환, IsFail = true, 오류 메시지 "0은 허용되지 않습니다"

#### `Divide_ShouldBePureFunction_WhenDenominatorIsNotZero`
- **목적**: 같은 입력에 대해 항상 동일한 결과를 반환하는 순수 함수 특성 확인
- **입력**: numerator = 10, denominator = 2
- **결과**: 3번의 연속 호출에서 모두 동일한 `Fin<int>.Succ(5)` 반환

#### `Divide_ShouldBePureFunction_WhenDenominatorIstZero`
- **목적**: 실패 케이스에서도 순수 함수 특성 확인
- **입력**: numerator = 10, denominator = 0
- **결과**: 3번의 연속 호출에서 모두 동일한 `Fin<int>.Fail(Error)` 반환, 오류 메시지 "0은 허용되지 않습니다"

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 4
- **성공**: 4
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 1개 테스트 케이스
- **실패 케이스**: 1개 테스트 케이스
- **순수성 테스트**: 2개 테스트 케이스 (성공/실패 각각)

### 학습 목표 달성도
- ✅ **함수형 결과 타입의 장점 이해**: 예외 대신 명시적인 성공/실패 표현 체험
- ✅ **`Fin<T>` 타입의 활용법 습득**: IsSucc, IsFail, Match 패턴 검증
- ✅ **순수 함수의 중요성 인식**: 같은 입력에 대한 일관된 결과 반환 확인
