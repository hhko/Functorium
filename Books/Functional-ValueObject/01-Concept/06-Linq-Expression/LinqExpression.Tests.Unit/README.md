# LinqExpression.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `LinqExpression` 프로젝트의 `Denominator` 값 객체와 `MathOperations` 클래스에 대한 단위 테스트를 포함합니다. LINQ 표현식을 통한 함수형 에러 처리와 연산자 오버로딩의 조합을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `LinqExpressionDivision_ShouldReturnCorrectResult_WhenUsingValidDenominator`
- **목적**: LINQ 표현식을 사용하여 유효한 Denominator로 나눗셈 연산을 수행할 때 정확한 결과를 반환하는지 확인
- **입력**: numerator = 10, denominatorValue = 2
- **결과**: 5

#### `LinqExpressionChaining_ShouldReturnCorrectResult_WhenUsingMultipleOperations`
- **목적**: LINQ 표현식을 사용한 다단계 연산 체이닝이 정확한 결과를 반환하는지 확인
- **입력**: numerator = 100, denominator1Value = 10, denominator2Value = 2, denominator3Value = 5
- **결과**: 1 (100 / 10 / 2 / 5 = 1)

### 예외/실패 케이스 테스트

#### `LinqExpressionDivision_ShouldReturnFailureResult_WhenUsingInvalidDenominator`
- **목적**: LINQ 표현식에서 유효하지 않은 Denominator 생성 시 실패 결과를 반환하는지 확인
- **입력**: numerator = 10, invalidDenominatorValue = 0
- **결과**: `Fin<int>.Fail` with message "0은 허용되지 않습니다"

#### `LinqExpressionErrorPropagation_ShouldReturnFailureResult_WhenAnyStepFails`
- **목적**: LINQ 표현식에서 중간 단계에서 실패가 발생하면 에러가 자동으로 전파되는지 확인
- **입력**: numerator = 100, validDenominator1Value = 10, invalidDenominatorValue = 0, validDenominator2Value = 5
- **결과**: `Fin<int>.Fail` with message "0은 허용되지 않습니다"

### LINQ 표현식 특수 패턴 테스트

#### `LinqExpressionMathOperations_ShouldReturnCorrectResult_WhenUsingCombinedOperations`
- **목적**: LINQ 표현식과 MathOperations.Divide 메서드를 조합한 연산이 정확한 결과를 반환하는지 확인
- **테스트 케이스**:
  - `(30, 6, 2) → 2`
  - `(60, 12, 3) → 1`
  - `(90, 18, 5) → 1`

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 7
- **성공**: 7
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 2개 테스트 케이스
- **예외/실패 케이스**: 2개 테스트 케이스
- **LINQ 표현식 특수 패턴 테스트**: 3개 테스트 케이스 (Theory 테스트)

### 학습 목표 달성도
- ✅ **LINQ 표현식을 통한 함수형 에러 처리 패턴 이해**: `from` 키워드를 사용한 모나딕 연산 체이닝 테스트 완료
- ✅ **연산자 오버로딩과 LINQ 표현식의 조합 이해**: 다양한 연산자 조합과 LINQ 표현식의 상호작용 테스트 완료
- ✅ **복합 연산에서의 에러 전파 자동화 이해**: LINQ 표현식을 통한 자연스러운 에러 처리와 연쇄 연산 테스트 완료
- ✅ **실제 사용 시나리오 기반 통합 테스트**: MathOperations.Divide와 LINQ 표현식의 조합 테스트 완료

모든 테스트가 성공적으로 통과하여 LINQ 표현식을 통한 함수형 에러 처리와 연산자 오버로딩의 조합이 올바르게 동작함을 확인했습니다.
