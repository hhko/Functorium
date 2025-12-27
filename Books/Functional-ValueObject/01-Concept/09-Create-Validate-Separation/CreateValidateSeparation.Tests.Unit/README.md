# CreateValidateSeparation.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `Denominator` 클래스의 Create와 Validate 분리 패턴에 대한 단위 테스트를 포함합니다. 단일 책임 원칙을 통한 검증과 생성 책임 분리, 함수형 합성을 통한 안전한 객체 생성, 그리고 검증 로직의 재사용성을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `Validate_ShouldReturnSuccess_WhenValueIsNotZero`
- **목적**: 분모가 0이 아닌 경우 Validate 메서드가 성공 결과를 반환하는지 확인
- **입력**: Theory 테스트로 다양한 유효한 값들 (1, 5, 10, -3, -10)
- **결과**: 각 입력값에 대해 Validation<Error, int>의 Success 케이스 반환

### 예외/실패 케이스 테스트

#### `Validate_ShouldReturnFailure_WhenValueIsZero`
- **목적**: 분모가 0인 경우 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: value = 0
- **결과**: Validation<Error, int>의 Failure 케이스 반환, 에러 메시지 "0은 허용되지 않습니다"

### 책임 분리 테스트

#### `Create_ShouldUseSameValidationLogic_AsValidate`
- **목적**: Create와 Validate 메서드가 동일한 검증 로직을 사용하는지 검증
- **입력**: Theory 테스트로 다양한 값들 (1, 5, 0, -3)
- **결과**: Validate와 Create 메서드의 성공/실패 결과가 일치하고, 실패 시 동일한 에러 메시지 반환

### 순수성 테스트

#### `Validate_ShouldBePureFunction_WhenCalledMultipleTimes`
- **목적**: Validate 메서드가 순수 함수로 동작하는지 검증 (동일한 입력에 대해 동일한 결과 반환)
- **입력**: value = 5
- **결과**: 동일한 값에 대해 두 번 호출한 결과가 동일

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 11
- **성공**: 11
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 5개 (Validate_ShouldReturnSuccess_WhenValueIsNotZero의 Theory 테스트)
- **예외/실패 케이스**: 1개 (Validate_ShouldReturnFailure_WhenValueIsZero)
- **책임 분리 테스트**: 4개 (Create_ShouldUseSameValidationLogic_AsValidate의 Theory 테스트)
- **순수성 테스트**: 1개 (Validate_ShouldBePureFunction_WhenCalledMultipleTimes)

### 학습 목표 달성도
- ✅ **단일 책임 원칙 적용**: Create와 Validate 메서드의 책임 분리 검증 완료
- ✅ **검증 로직의 재사용성**: Validate 메서드의 독립적 사용 가능성 확인
- ✅ **함수형 합성 활용**: LanguageExt의 Validation과 Fin을 활용한 안전한 값 생성 검증
- ✅ **순수성 보장**: Validate 메서드의 순수 함수 특성 검증
