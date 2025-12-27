# ValidatedValueCreation.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `ValidatedValueCreation` 값 객체들에 대한 단위 테스트를 포함합니다. 검증된 값 생성 패턴의 3가지 메서드 패턴(Create, Validate, CreateFromValidated)과 복합 검증의 AND 조건 특성을 모두 테스트합니다.

## 테스트 시나리오

### AddressTests (복합 값 객체 테스트)

#### `Create_ShouldReturnSuccess_WhenAllComponentsAreValid`
- **목적**: 모든 구성 요소가 유효한 경우 Address 객체가 정상 생성되는지 확인
- **입력**: street = "123 Main St", city = "Seoul", postalCode = "12345"
- **결과**: Address 객체 생성 성공
- **테스트 케이스**:
  - `("123 Main St", "Seoul", "12345")`
  - `("Broadway", "New York", "10001")`
  - `("서울시 강남구 테헤란로", "서울", "123456")`

#### `Validate_ShouldReturnSuccess_WhenAllComponentsAreValid`
- **목적**: 모든 구성 요소가 유효한 경우 Validate 메서드가 성공 결과를 반환하는지 확인
- **입력**: street = "123 Main St", city = "Seoul", postalCode = "12345"
- **결과**: 검증된 값 튜플 반환 성공

#### `CreateFromValidated_ShouldCreateAddress_WhenValidatedValueObjectsAreProvided`
- **목적**: 검증된 값 객체들로 직접 Address 객체를 생성하는지 확인
- **입력**: 검증된 Street, City, PostalCode 객체들
- **결과**: Address 객체 생성 성공

#### `Validate_ShouldBePureFunction_WhenCalledMultipleTimes`
- **목적**: Validate 메서드가 순수 함수로 동작하는지 검증
- **입력**: street = "123 Main St", city = "Seoul", postalCode = "12345"
- **결과**: 동일한 입력에 대해 항상 동일한 결과 반환

#### `Create_ShouldUseSameValidationLogic_AsValidate`
- **목적**: Create와 Validate가 동일한 검증 로직을 사용하는지 검증
- **입력**: 다양한 유효/무효 조합
- **결과**: Create와 Validate의 성공/실패 결과가 일치

#### `Create_ShouldReturnFailure_WhenStreetIsEmpty`
- **목적**: 거리명이 빈 경우 Address 생성 시 실패하는지 확인
- **입력**: street = "", city = "Seoul", postalCode = "12345"
- **결과**: "거리명은 비어있을 수 없습니다" 에러 메시지
- **테스트 케이스**:
  - `("", "Seoul", "12345")`
  - `("   ", "Seoul", "12345")`

#### `Create_ShouldReturnFailure_WhenCityIsEmpty`
- **목적**: 도시명이 빈 경우 Address 생성 시 실패하는지 확인
- **입력**: street = "123 Main St", city = "", postalCode = "12345"
- **결과**: "도시명은 비어있을 수 없습니다" 에러 메시지
- **테스트 케이스**:
  - `("123 Main St", "", "12345")`
  - `("123 Main St", "   ", "12345")`
  - `("123 Main St", null, "12345")`

#### `Create_ShouldReturnFailure_WhenPostalCodeIsEmpty`
- **목적**: 우편번호가 유효하지 않은 경우 Address 생성 시 실패하는지 확인
- **입력**: street = "123 Main St", city = "Seoul", postalCode = ""
- **결과**: "우편번호는 비어있을 수 없습니다" 에러 메시지
- **테스트 케이스**:
  - `("123 Main St", "Seoul", "")`
  - `("123 Main St", "Seoul", "   ")`
  - `("123 Main St", "Seoul", null)`

### StreetTests (단일 값 객체 테스트)

#### `Create_ShouldReturnSuccess_WhenStreetNameIsValid`
- **목적**: 유효한 거리명으로 Street 객체가 정상 생성되는지 확인
- **입력**: streetName = "123 Main St"
- **결과**: Street 객체 생성 성공
- **테스트 케이스**:
  - `("123 Main St")`
  - `("Broadway")`
  - `("서울시 강남구 테헤란로")`

#### `Create_ShouldReturnFailure_WhenStreetNameIsEmpty`
- **목적**: 빈 거리명으로 Street 생성 시 실패하는지 확인
- **입력**: streetName = ""
- **결과**: "거리명은 비어있을 수 없습니다" 에러 메시지
- **테스트 케이스**:
  - `("")`
  - `("   ")`

#### `Validate_ShouldReturnSuccess_WhenStreetNameIsValid`
- **목적**: 유효한 거리명에 대해 Validate 메서드가 성공 결과를 반환하는지 확인
- **입력**: streetName = "123 Main St"
- **결과**: 검증된 값 반환 성공

#### `Validate_ShouldReturnFailure_WhenStreetNameIsEmpty`
- **목적**: 빈 거리명에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: streetName = ""
- **결과**: "거리명은 비어있을 수 없습니다" 에러 메시지

#### `CreateFromValidated_ShouldCreateStreet_WhenValidatedValueIsProvided`
- **목적**: 검증된 값으로 직접 Street 객체를 생성하는지 확인
- **입력**: validatedValue = "123 Main St"
- **결과**: Street 객체 생성 성공

#### `Create_ShouldUseSameValidationLogic_AsValidate`
- **목적**: Create와 Validate가 동일한 검증 로직을 사용하는지 검증
- **입력**: 다양한 유효/무효 값
- **결과**: Create와 Validate의 성공/실패 결과가 일치

#### `Create_ShouldReturnFailure_WhenStreetNameIsNull`
- **목적**: null 거리명으로 Street 생성 시 실패하는지 확인
- **입력**: streetName = null
- **결과**: "거리명은 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldReturnFailure_WhenStreetNameIsNull`
- **목적**: null 거리명에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: streetName = null
- **결과**: "거리명은 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldBePureFunction_WhenCalledMultipleTimes`
- **목적**: Validate 메서드가 순수 함수로 동작하는지 검증
- **입력**: streetName = "123 Main St"
- **결과**: 동일한 입력에 대해 항상 동일한 결과 반환

### CityTests (단일 값 객체 테스트)

#### `Create_ShouldReturnSuccess_WhenCityNameIsValid`
- **목적**: 유효한 도시명으로 City 객체가 정상 생성되는지 확인
- **입력**: cityName = "Seoul"
- **결과**: City 객체 생성 성공
- **테스트 케이스**:
  - `("Seoul")`
  - `("New York")`
  - `("서울")`
  - `("부산")`

#### `Create_ShouldReturnFailure_WhenCityNameIsEmpty`
- **목적**: 빈 도시명으로 City 생성 시 실패하는지 확인
- **입력**: cityName = ""
- **결과**: "도시명은 비어있을 수 없습니다" 에러 메시지
- **테스트 케이스**:
  - `("")`
  - `("   ")`

#### `Validate_ShouldReturnSuccess_WhenCityNameIsValid`
- **목적**: 유효한 도시명에 대해 Validate 메서드가 성공 결과를 반환하는지 확인
- **입력**: cityName = "Seoul"
- **결과**: 검증된 값 반환 성공

#### `Validate_ShouldReturnFailure_WhenCityNameIsEmpty`
- **목적**: 빈 도시명에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: cityName = ""
- **결과**: "도시명은 비어있을 수 없습니다" 에러 메시지

#### `CreateFromValidated_ShouldCreateCity_WhenValidatedValueIsProvided`
- **목적**: 검증된 값으로 직접 City 객체를 생성하는지 확인
- **입력**: validatedValue = "Seoul"
- **결과**: City 객체 생성 성공

#### `Create_ShouldUseSameValidationLogic_AsValidate`
- **목적**: Create와 Validate가 동일한 검증 로직을 사용하는지 검증
- **입력**: 다양한 유효/무효 값
- **결과**: Create와 Validate의 성공/실패 결과가 일치

#### `Create_ShouldReturnFailure_WhenCityNameIsNull`
- **목적**: null 도시명으로 City 생성 시 실패하는지 확인
- **입력**: cityName = null
- **결과**: "도시명은 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldReturnFailure_WhenCityNameIsNull`
- **목적**: null 도시명에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: cityName = null
- **결과**: "도시명은 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldBePureFunction_WhenCalledMultipleTimes`
- **목적**: Validate 메서드가 순수 함수로 동작하는지 검증
- **입력**: cityName = "Seoul"
- **결과**: 동일한 입력에 대해 항상 동일한 결과 반환

### PostalCodeTests (단일 값 객체 테스트)

#### `Create_ShouldReturnSuccess_WhenPostalCodeIsValid`
- **목적**: 유효한 우편번호로 PostalCode 객체가 정상 생성되는지 확인
- **입력**: postalCode = "12345"
- **결과**: PostalCode 객체 생성 성공
- **테스트 케이스**:
  - `("12345")`
  - `("123456")`
  - `("1234567")`
  - `("12345678")`

#### `Create_ShouldReturnFailure_WhenPostalCodeIsEmpty`
- **목적**: 빈 우편번호로 PostalCode 생성 시 실패하는지 확인
- **입력**: postalCode = ""
- **결과**: "우편번호는 비어있을 수 없습니다" 에러 메시지
- **테스트 케이스**:
  - `("")`
  - `("   ")`

#### `Create_ShouldReturnFailure_WhenPostalCodeContainsNonDigits`
- **목적**: 숫자가 아닌 문자가 포함된 우편번호로 PostalCode 생성 시 실패하는지 확인
- **입력**: postalCode = "1234a"
- **결과**: "우편번호는 숫자만 포함해야 합니다" 에러 메시지
- **테스트 케이스**:
  - `("1234a")`
  - `("abc123")`
  - `("123-456")`
  - `("123 456")`

#### `Validate_ShouldReturnSuccess_WhenPostalCodeIsValid`
- **목적**: 유효한 우편번호에 대해 Validate 메서드가 성공 결과를 반환하는지 확인
- **입력**: postalCode = "12345"
- **결과**: 검증된 값 반환 성공

#### `Validate_ShouldReturnFailure_WhenPostalCodeIsEmpty`
- **목적**: 빈 우편번호에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: postalCode = ""
- **결과**: "우편번호는 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldReturnFailure_WhenPostalCodeContainsNonDigits`
- **목적**: 숫자가 아닌 문자가 포함된 우편번호에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: postalCode = "1234a"
- **결과**: "우편번호는 숫자만 포함해야 합니다" 에러 메시지

#### `CreateFromValidated_ShouldCreatePostalCode_WhenValidatedValueIsProvided`
- **목적**: 검증된 값으로 직접 PostalCode 객체를 생성하는지 확인
- **입력**: validatedValue = "12345"
- **결과**: PostalCode 객체 생성 성공

#### `Create_ShouldUseSameValidationLogic_AsValidate`
- **목적**: Create와 Validate가 동일한 검증 로직을 사용하는지 검증
- **입력**: 다양한 유효/무효 값
- **결과**: Create와 Validate의 성공/실패 결과가 일치

#### `Create_ShouldReturnFailure_WhenPostalCodeIsNull`
- **목적**: null 우편번호로 PostalCode 생성 시 실패하는지 확인
- **입력**: postalCode = null
- **결과**: "우편번호는 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldReturnFailure_WhenPostalCodeIsNull`
- **목적**: null 우편번호에 대해 Validate 메서드가 실패 결과를 반환하는지 확인
- **입력**: postalCode = null
- **결과**: "우편번호는 비어있을 수 없습니다" 에러 메시지

#### `Validate_ShouldBePureFunction_WhenCalledMultipleTimes`
- **목적**: Validate 메서드가 순수 함수로 동작하는지 검증
- **입력**: postalCode = "12345"
- **결과**: 동일한 입력에 대해 항상 동일한 결과 반환

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 91
- **성공**: 91
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 25개 (Create 성공, Validate 성공, CreateFromValidated 성공)
- **예외/실패 케이스**: 36개 (빈 값, null 값, 잘못된 형식)
- **엣지 케이스**: 12개 (공백 문자열, null 값)
- **순수성 테스트**: 4개 (Validate 메서드의 순수성 검증)
- **특수 패턴 테스트**: 14개 (Create와 Validate 로직 일치성, 복합 검증의 AND 조건)

### 학습 목표 달성도
- ✅ **검증된 값 생성 패턴 이해**: Create, Validate, CreateFromValidated 3가지 메서드 패턴 완전 검증
- ✅ **복합 검증의 AND 조건 특성 이해**: 하나의 구성 요소라도 유효하지 않으면 전체 검증 실패 확인
- ✅ **단일 책임 원칙 준수**: 검증 로직과 객체 생성 로직의 분리 확인
- ✅ **함수형 프로그래밍 패턴 이해**: LanguageExt의 Validation 모나드와 Fin 타입 활용 확인
- ✅ **순수성 보장**: Validate 메서드의 순수 함수 특성 검증
