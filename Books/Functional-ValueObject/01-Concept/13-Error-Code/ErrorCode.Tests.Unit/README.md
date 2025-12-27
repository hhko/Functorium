# ErrorCode.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `ErrorCode` 에러 코드 관리 시스템에 대한 단위 테스트를 포함합니다. ErrorCodeFactory의 다양한 에러 생성 기능과 값 객체의 에러 처리 기능을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `ErrorCodeFactoryTests`
- **목적**: ErrorCodeFactory의 다양한 에러 생성 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringParameters`: 문자열 에러 코드와 문자열 값을 사용하여 기본 에러 생성 검증
    - **입력**: errorCode="DomainErrors.Name.Invalid", errorCurrentValue="invalid@name"
    - **결과**: ErrorCodeExpected 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringAndIntParameters`: 문자열 에러 코드와 정수 값을 사용하여 기본 에러 생성 검증
    - **입력**: errorCode="DomainErrors.Age.Invalid", errorCurrentValue=150
    - **결과**: ErrorCodeExpected 타입의 에러 객체 생성 (정수 값이 문자열로 변환)
  - `Create_ShouldReturnErrorCodeExpectedWithGenericType_WhenUsingGenericMethod`: 제네릭 타입을 사용하여 타입 안전한 에러 생성 검증
    - **테스트 케이스**: ("DomainErrors.Email.Invalid", "not-an-email"), ("DomainErrors.Phone.Invalid", "invalid-phone"), ("DomainErrors.Address.Invalid", "empty-address")
    - **결과**: `ErrorCodeExpected<string>` 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpectedWithTwoGenericTypes_WhenUsingTwoValueMethod`: 두 개의 제네릭 타입을 사용하여 다중 값 에러 생성 검증
    - **입력**: errorCode="DomainErrors.Coordinate.OutOfRange", errorCurrentValue1=1500, errorCurrentValue2=2000
    - **결과**: `ErrorCodeExpected<int, int>` 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpectedWithThreeGenericTypes_WhenUsingThreeValueMethod`: 세 개의 제네릭 타입을 사용하여 다중 값 에러 생성 검증
    - **입력**: errorCode="DomainErrors.Address.Invalid", errorCurrentValue1="Empty Street", errorCurrentValue2="Invalid City", errorCurrentValue3="12345"
    - **결과**: `ErrorCodeExpected<string, string, string>` 타입의 에러 객체 생성
  - `CreateFromException_ShouldReturnErrorCodeExceptional_WhenUsingException`: 예외를 사용하여 예외 기반 에러 생성 검증
    - **입력**: errorCode="DomainErrors.System.Exception", exception=InvalidOperationException("Test exception message")
    - **결과**: ErrorCodeExceptional 타입의 에러 객체 생성
  - `Format_ShouldReturnFormattedErrorCode_WhenUsingStringArray`: 여러 문자열을 점으로 연결하여 에러 코드 포맷팅 검증
    - **테스트 케이스**: (["DomainErrors", "User", "InvalidAge"], "DomainErrors.User.InvalidAge"), (["DomainErrors", "Payment", "Declined"], "DomainErrors.Payment.Declined"), (["DomainErrors", "Order", "NotFound"], "DomainErrors.Order.NotFound")
    - **결과**: 올바른 형식의 에러 코드 문자열 반환

#### `DenominatorTests`
- **목적**: 값 객체의 에러 처리 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnFailureResult_WhenValueIsZero`: 0 값으로 Denominator 생성 시 실패 검증
    - **입력**: value=0
    - **결과**: 실패 및 ErrorCodeExpected 타입의 에러 객체 반환

### 예외/실패 케이스 테스트

#### `ErrorCodeFactoryTests`
- **목적**: ErrorCodeFactory의 에러 처리 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringParameters`: 문자열 파라미터로 에러 생성 시 정상 처리 검증
    - **입력**: 유효한 문자열 파라미터
    - **결과**: ErrorCodeExpected 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringAndIntParameters`: 문자열과 정수 파라미터로 에러 생성 시 정상 처리 검증
    - **입력**: 유효한 문자열과 정수 파라미터
    - **결과**: ErrorCodeExpected 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpectedWithGenericType_WhenUsingGenericMethod`: 제네릭 메서드로 에러 생성 시 정상 처리 검증
    - **입력**: 유효한 제네릭 타입 파라미터
    - **결과**: `ErrorCodeExpected<T>` 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpectedWithTwoGenericTypes_WhenUsingTwoValueMethod`: 두 개의 제네릭 타입으로 에러 생성 시 정상 처리 검증
    - **입력**: 유효한 두 개의 제네릭 타입 파라미터
    - **결과**: `ErrorCodeExpected<T1, T2>` 타입의 에러 객체 생성
  - `Create_ShouldReturnErrorCodeExpectedWithThreeGenericTypes_WhenUsingThreeValueMethod`: 세 개의 제네릭 타입으로 에러 생성 시 정상 처리 검증
    - **입력**: 유효한 세 개의 제네릭 타입 파라미터
    - **결과**: `ErrorCodeExpected<T1, T2, T3>` 타입의 에러 객체 생성
  - `CreateFromException_ShouldReturnErrorCodeExceptional_WhenUsingException`: 예외로 에러 생성 시 정상 처리 검증
    - **입력**: 유효한 예외 객체
    - **결과**: ErrorCodeExceptional 타입의 에러 객체 생성
  - `Format_ShouldReturnFormattedErrorCode_WhenUsingStringArray`: 문자열 배열로 에러 코드 포맷팅 시 정상 처리 검증
    - **입력**: 유효한 문자열 배열
    - **결과**: 올바른 형식의 에러 코드 문자열 반환

#### `DenominatorTests`
- **목적**: 값 객체의 에러 처리 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnFailureResult_WhenValueIsZero`: 무효한 값으로 Denominator 생성 시 에러 처리 검증
    - **입력**: value=0 (무효한 값)
    - **결과**: 실패 및 ErrorCodeExpected 타입의 에러 객체 반환

### 엣지 케이스 테스트

#### `ErrorCodeFactoryTests`
- **목적**: 경계값 파라미터로 에러 생성 검증
- **테스트 메서드**:
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringAndIntParameters`: 정수 값을 문자열로 변환하는 경계값 처리 검증
    - **입력**: errorCurrentValue=150 (정수)
    - **결과**: "150" (문자열로 변환) 반환
  - `Create_ShouldReturnErrorCodeExpectedWithTwoGenericTypes_WhenUsingTwoValueMethod`: 두 개의 정수 값을 사용하는 경계값 처리 검증
    - **입력**: errorCurrentValue1=1500, errorCurrentValue2=2000
    - **결과**: 두 개의 정수 값이 올바르게 저장됨
  - `Create_ShouldReturnErrorCodeExpectedWithThreeGenericTypes_WhenUsingThreeValueMethod`: 세 개의 문자열 값을 사용하는 경계값 처리 검증
    - **입력**: errorCurrentValue1="Empty Street", errorCurrentValue2="Invalid City", errorCurrentValue3="12345"
    - **결과**: 세 개의 문자열 값이 올바르게 저장됨

#### `DenominatorTests`
- **목적**: 경계값으로 Denominator 생성 검증
- **테스트 메서드**:
  - `Create_ShouldReturnFailureResult_WhenValueIsZero`: 0 값으로 Denominator 생성 시 경계값 처리 검증
    - **입력**: value=0 (경계값)
    - **결과**: 실패 및 적절한 에러 코드 반환

### 순수성 테스트

#### `ErrorCodeFactoryTests`
- **목적**: ErrorCodeFactory의 순수성 검증
- **테스트 메서드**:
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringParameters`: 문자열 파라미터로 에러 생성의 순수성 검증
    - **입력**: 동일한 문자열 파라미터
    - **결과**: 예측 가능한 ErrorCodeExpected 객체 반환
  - `Create_ShouldReturnErrorCodeExpected_WhenUsingStringAndIntParameters`: 문자열과 정수 파라미터로 에러 생성의 순수성 검증
    - **입력**: 동일한 문자열과 정수 파라미터
    - **결과**: 예측 가능한 ErrorCodeExpected 객체 반환
  - `Create_ShouldReturnErrorCodeExpectedWithGenericType_WhenUsingGenericMethod`: 제네릭 메서드로 에러 생성의 순수성 검증
    - **입력**: 동일한 제네릭 타입 파라미터
    - **결과**: 예측 가능한 `ErrorCodeExpected<T>` 객체 반환
  - `Create_ShouldReturnErrorCodeExpectedWithTwoGenericTypes_WhenUsingTwoValueMethod`: 두 개의 제네릭 타입으로 에러 생성의 순수성 검증
    - **입력**: 동일한 두 개의 제네릭 타입 파라미터
    - **결과**: 예측 가능한 `ErrorCodeExpected<T1, T2>` 객체 반환
  - `Create_ShouldReturnErrorCodeExpectedWithThreeGenericTypes_WhenUsingThreeValueMethod`: 세 개의 제네릭 타입으로 에러 생성의 순수성 검증
    - **입력**: 동일한 세 개의 제네릭 타입 파라미터
    - **결과**: 예측 가능한 `ErrorCodeExpected<T1, T2, T3>` 객체 반환
  - `CreateFromException_ShouldReturnErrorCodeExceptional_WhenUsingException`: 예외로 에러 생성의 순수성 검증
    - **입력**: 동일한 예외 객체
    - **결과**: 예측 가능한 ErrorCodeExceptional 객체 반환
  - `Format_ShouldReturnFormattedErrorCode_WhenUsingStringArray`: 문자열 배열로 에러 코드 포맷팅의 순수성 검증
    - **입력**: 동일한 문자열 배열
    - **결과**: 예측 가능한 에러 코드 문자열 반환

#### `DenominatorTests`
- **목적**: 값 객체의 에러 처리 순수성 검증
- **테스트 메서드**:
  - `Create_ShouldReturnFailureResult_WhenValueIsZero`: 0 값으로 Denominator 생성의 순수성 검증
    - **입력**: 동일한 0 값
    - **결과**: 예측 가능한 에러 객체 반환

### 특수 패턴 테스트

#### `ErrorCodeFactoryTests`
- **목적**: Theory와 InlineData를 사용한 다중 테스트 케이스 검증
- **테스트 메서드**:
  - `Create_ShouldReturnErrorCodeExpectedWithGenericType_WhenUsingGenericMethod`: 다중 제네릭 타입 테스트
    - **테스트 케이스**: 3개의 제네릭 타입 케이스
    - **결과**: 모든 케이스에 대한 성공적인 `ErrorCodeExpected<string>` 생성
  - `Format_ShouldReturnFormattedErrorCode_WhenUsingStringArray`: 다중 에러 코드 포맷팅 테스트
    - **테스트 케이스**: 3개의 에러 코드 포맷팅 케이스
    - **결과**: 모든 케이스에 대한 올바른 에러 코드 문자열 반환

#### `DenominatorTests`
- **목적**: 값 객체의 에러 처리 패턴 테스트
- **테스트 메서드**:
  - `Create_ShouldReturnFailureResult_WhenValueIsZero`: 0 값으로 Denominator 생성 패턴 테스트
    - **입력**: 0 값
    - **결과**: ErrorCodeExpected 타입의 에러 객체 반환

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 8개
- **성공**: 8개
- **실패**: 0개
- **건너뜀**: 0개

### 테스트 커버리지
- **정상 케이스**: 2개 테스트 케이스
- **예외/실패 케이스**: 2개 테스트 케이스
- **엣지 케이스**: 2개 테스트 케이스
- **순수성 테스트**: 1개 테스트 케이스
- **특수 패턴 테스트**: 1개 테스트 케이스

### 학습 목표 달성도
- ✅ **핵심 개념 이해**: ErrorCodeFactory의 다양한 에러 생성 기능 이해
- ✅ **타입 안전성**: 제네릭 타입을 활용한 타입 안전한 에러 생성 이해
- ✅ **에러 코드 포맷팅**: 점으로 연결된 에러 코드 포맷팅 기능 이해
- ✅ **예외 처리**: 예외 기반 에러 생성과 처리 이해
- ✅ **값 객체 통합**: 값 객체와 에러 코드 시스템의 통합 이해
- ✅ **다중 값 에러**: 여러 값을 포함하는 복합 에러 생성 이해
