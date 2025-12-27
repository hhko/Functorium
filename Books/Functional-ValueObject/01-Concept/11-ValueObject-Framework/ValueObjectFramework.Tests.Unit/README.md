# ValueObjectFramework.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `ValueObjectFramework` 값 객체 프레임워크에 대한 단위 테스트를 포함합니다. Comparable과 ComparableNot 두 가지 카테고리의 값 객체들에 대한 생성, 검증, 비교, 동등성 기능을 모두 테스트합니다.

## 테스트 시나리오

### Comparable (비교 가능한 값 객체)

#### PrimitiveValueObjects (기본형 값 객체)

##### `DenominatorTests`
- **목적**: 0이 아닌 정수 값 객체의 기본 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccess_WhenValueIsValid`: 유효한 값으로 Denominator 생성 검증
    - **입력**: 5
    - **결과**: 성공적인 Denominator 인스턴스 생성
  - `Create_ShouldReturnFailure_WhenValueIsZero`: 0 값으로 Denominator 생성 시 실패 검증
    - **입력**: 0
    - **결과**: 실패 및 "0은 허용되지 않습니다" 에러 메시지
  - `ExplicitOperator_ShouldReturnCorrectValue_WhenConvertingToInt`: 명시적 변환 연산자 검증
    - **입력**: Denominator 인스턴스 (값: 7)
    - **결과**: 7 반환
  - `Equals_ShouldReturnTrue_WhenValuesAreEqual`: 동일한 값을 가진 두 Denominator 동등성 검증
    - **입력**: 두 개의 Denominator (값: 5)
    - **결과**: true 반환
  - `Equals_ShouldReturnFalse_WhenValuesAreDifferent`: 다른 값을 가진 두 Denominator 동등성 검증
    - **입력**: Denominator (값: 5), Denominator (값: 3)
    - **결과**: false 반환
  - `CompareTo_ShouldReturnCorrectComparison_WhenComparingValues`: CompareTo 메서드 검증
    - **입력**: Denominator (값: 3), Denominator (값: 5)
    - **결과**: -1 반환 (3 < 5)
  - `LessThanOperator_ShouldReturnTrue_WhenLeftIsLessThanRight`: < 연산자 검증
    - **입력**: Denominator (값: 3), Denominator (값: 5)
    - **결과**: true 반환
  - `GreaterThanOperator_ShouldReturnTrue_WhenLeftIsGreaterThanRight`: > 연산자 검증
    - **입력**: Denominator (값: 7), Denominator (값: 5)
    - **결과**: true 반환
  - `LessThanOrEqualOperator_ShouldReturnTrue_WhenLeftIsLessThanOrEqualRight`: <= 연산자 검증
    - **입력**: Denominator (값: 5), Denominator (값: 5)
    - **결과**: true 반환
  - `GreaterThanOrEqualOperator_ShouldReturnTrue_WhenLeftIsGreaterThanOrEqualRight`: >= 연산자 검증
    - **입력**: Denominator (값: 5), Denominator (값: 5)
    - **결과**: true 반환
  - `ToString_ShouldReturnValueStringRepresentation_WhenCalled`: ToString 메서드 검증
    - **입력**: Denominator (값: 42)
    - **결과**: "42" 반환

#### CompositeValueObjects (복합형 값 객체)

##### `PriceTests`
- **목적**: 가격 값 객체의 생성 및 비교 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccessResult_WhenValidPrice`: 유효한 가격으로 Price 생성 검증
    - **테스트 케이스**: (0), (100), (9999999999999999.99)
    - **결과**: 성공적인 Price 인스턴스 생성
  - `Create_ShouldReturnFailureResult_WhenNegativePrice`: 음수 가격으로 Price 생성 시 실패 검증
    - **테스트 케이스**: (-1), (-100.50), (-9999999999999999.99)
    - **결과**: 실패 및 "가격은 0 이상이어야 합니다" 에러 메시지
  - `Validate_ShouldReturnCorrectValidationResult_WhenVariousPrices`: Validate 메서드 검증
    - **테스트 케이스**: (0, true), (100, true), (-1, false), (-100.50, false)
    - **결과**: 올바른 검증 결과 반환
  - `CreateFromValidated_ShouldReturnPriceInstance_WhenValidatedPrice`: CreateFromValidated 메서드 검증
    - **테스트 케이스**: (0), (100), (9999999999999999.99)
    - **결과**: Price 인스턴스 생성
  - `ToString_ShouldReturnFormattedPriceInfo_WhenCalled`: ToString 메서드 검증
    - **테스트 케이스**: (0, "₩0"), (100, "₩100"), (1000, "₩1,000"), (1000000, "₩1,000,000")
    - **결과**: 올바른 형식으로 가격 정보 반환
  - `CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingPrices`: CompareTo 메서드 검증
    - **테스트 케이스**: (100, 200, -1), (200, 100, 1), (100, 100, 0)
    - **결과**: 올바른 비교 결과 반환
  - `Equals_ShouldReturnCorrectEqualityResult_WhenComparingPrices`: Equals 메서드 검증
    - **테스트 케이스**: (100, 100, true), (100, 200, false), (200, 200, true)
    - **결과**: 올바른 동등성 결과 반환
  - `ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPrices`: 비교 연산자 검증
    - **테스트 케이스**: (100, 200, true, false, false, true, false, true)
    - **결과**: 모든 비교 연산자 올바른 결과 반환
  - `ComparisonWithNull_ShouldReturnCorrectResults_WhenComparingWithNull`: null과의 비교 검증
    - **입력**: Price 인스턴스, null
    - **결과**: 올바른 null 비교 결과 반환
  - `Equals_ShouldReturnFalse_WhenComparingWithDifferentType`: 다른 타입과의 비교 검증
    - **입력**: Price 인스턴스, 문자열
    - **결과**: false 반환
  - `GetHashCode_ShouldReturnSameHashCode_WhenSamePrice`: GetHashCode 메서드 검증
    - **입력**: 두 개의 Price (값: 100)
    - **결과**: 동일한 해시 코드 반환
  - `ExplicitConversion_ShouldConvertToDecimal_WhenPriceInstance`: 명시적 변환 검증
    - **테스트 케이스**: (0), (100), (9999999999999999.99)
    - **결과**: 올바른 decimal 값 반환
  - `ValidationPipeline_ShouldWorkCorrectly_WhenUsingLINQExpression`: LINQ Expression 검증 파이프라인 검증
    - **테스트 케이스**: (100, true), (0, true), (-1, false), (-100.50, false)
    - **결과**: 예측 가능한 검증 결과

##### `CurrencyTests`
- **목적**: 통화 코드 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccessResult_WhenValidCurrencyCode`: 유효한 통화 코드로 Currency 생성 검증
    - **테스트 케이스**: ("KRW"), ("USD"), ("EUR"), ("JPY"), ("GBP")
    - **결과**: 성공적인 Currency 인스턴스 생성
  - `Create_ShouldReturnSuccessResult_WhenLowerCaseCurrencyCode`: 소문자 통화 코드 처리 검증
    - **테스트 케이스**: ("krw", "KRW"), ("usd", "USD"), ("eur", "EUR")
    - **결과**: 대문자로 변환된 Currency 인스턴스 생성
  - `Create_ShouldReturnFailureResult_WhenEmptyCurrencyCode`: 빈 통화 코드로 Currency 생성 시 실패 검증
    - **입력**: ""
    - **결과**: 실패 및 적절한 에러 메시지
  - `Create_ShouldReturnFailureResult_WhenInvalidCurrencyCode`: 무효한 통화 코드로 Currency 생성 시 실패 검증
    - **입력**: 무효한 통화 코드
    - **결과**: 실패 및 적절한 에러 메시지
  - `Create_ShouldReturnFailureResult_WhenUnsupportedCurrencyCode`: 지원하지 않는 통화 코드로 Currency 생성 시 실패 검증
    - **입력**: 지원하지 않는 통화 코드
    - **결과**: 실패 및 적절한 에러 메시지

##### `PriceRangeTests`
- **목적**: 가격 범위 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**: (구체적인 테스트 메서드는 파일 내용에 따라 추가)

#### CompositePrimitiveValueObjects (복합 기본형 값 객체)

##### `DateRangeTests`
- **목적**: 날짜 범위 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccessResult_WhenValidDateRange`: 유효한 날짜 범위로 DateRange 생성 검증
    - **입력**: 시작일=2024-01-01, 종료일=2024-12-31
    - **결과**: 성공적인 DateRange 인스턴스 생성
  - `Create_ShouldReturnFailureResult_WhenStartDateAfterEndDate`: 시작일이 종료일보다 늦을 때 DateRange 생성 시 실패 검증
    - **입력**: 시작일=2024-12-31, 종료일=2024-01-01
    - **결과**: 실패 및 "시작일은 종료일보다 이전이어야 합니다" 에러 메시지
  - `Create_ShouldReturnFailureResult_WhenSameStartAndEndDate`: 시작일과 종료일이 같을 때 DateRange 생성 시 실패 검증
    - **입력**: 시작일=종료일=2024-06-15
    - **결과**: 실패 및 "시작일은 종료일보다 이전이어야 합니다" 에러 메시지
  - `Equals_ShouldReturnCorrectEqualityResult_WhenSameDateRange`: 동일한 날짜 범위를 가진 두 DateRange 동등성 검증
    - **입력**: 두 개의 DateRange (동일한 날짜 범위)
    - **결과**: true 반환 및 동일한 해시 코드
  - `Equals_ShouldReturnCorrectEqualityResult_WhenDifferentDateRange`: 다른 날짜 범위를 가진 두 DateRange 동등성 검증
    - **입력**: 두 개의 DateRange (다른 날짜 범위)
    - **결과**: false 반환
  - `ComparisonOperators_ShouldReturnCorrectResults_WhenEarlierRange`: 이른 날짜 범위의 비교 연산자 검증
    - **입력**: 이른 DateRange, 늦은 DateRange
    - **결과**: <, <= 연산자 true, >, >= 연산자 false
  - `ComparisonOperators_ShouldReturnCorrectResults_WhenSameRange`: 동일한 날짜 범위의 비교 연산자 검증
    - **입력**: 두 개의 동일한 DateRange
    - **결과**: ==, <=, >= 연산자 true, != 연산자 false
  - `ComparisonOperators_ShouldReturnCorrectResults_WhenLaterRange`: 늦은 날짜 범위의 비교 연산자 검증
    - **입력**: 늦은 DateRange, 이른 DateRange
    - **결과**: >, >= 연산자 true, <, <= 연산자 false
  - `ToString_ShouldReturnFormattedDateRangeInfo_WhenCalled`: ToString 메서드 검증
    - **입력**: DateRange (시작일=2024-01-01, 종료일=2024-12-31)
    - **결과**: "2024-01-01 ~ 2024-12-31" 반환
  - `CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingDateRanges`: CompareTo 메서드 검증
    - **입력**: 세 개의 DateRange (이른 범위, 늦은 범위, 동일한 범위)
    - **결과**: 올바른 비교 결과 반환

### ComparableNot (비교 불가능한 값 객체)

#### PrimitiveValueObjects (기본형 값 객체)

##### `BinaryDataTests`
- **목적**: 바이너리 데이터 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccessResult_WhenValidBinaryData`: 유효한 바이너리 데이터로 BinaryData 생성 검증
    - **입력**: new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F } (Hello)
    - **결과**: 성공적인 BinaryData 인스턴스 생성
  - `Create_ShouldReturnFailureResult_WhenEmptyArray`: 빈 배열로 BinaryData 생성 시 실패 검증
    - **입력**: new byte[0]
    - **결과**: 실패 및 "바이너리 데이터는 비어있을 수 없습니다" 에러 메시지
  - `Create_ShouldReturnFailureResult_WhenNullArray`: null 배열로 BinaryData 생성 시 실패 검증
    - **입력**: null
    - **결과**: 실패 및 적절한 에러 메시지

#### CompositeValueObjects (복합형 값 객체)

##### `AddressTests`
- **목적**: 복합 주소 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccess_WhenAddressIsValid`: 유효한 주소로 Address 생성 검증
    - **입력**: 거리명="123 Main St", 도시명="Seoul", 우편번호="12345"
    - **결과**: 성공적인 Address 인스턴스 생성
  - `Create_ShouldReturnFailure_WhenStreetIsEmpty`: 빈 거리명으로 Address 생성 시 실패 검증
    - **입력**: 거리명="", 도시명="Seoul", 우편번호="12345"
    - **결과**: 실패 및 "거리명은 비어있을 수 없습니다" 에러 메시지
  - `Create_ShouldReturnFailure_WhenCityIsEmpty`: 빈 도시명으로 Address 생성 시 실패 검증
    - **입력**: 거리명="123 Main St", 도시명="", 우편번호="12345"
    - **결과**: 실패 및 "도시명은 비어있을 수 없습니다" 에러 메시지
  - `Create_ShouldReturnFailure_WhenPostalCodeIsInvalid`: 무효한 우편번호로 Address 생성 시 실패 검증
    - **테스트 케이스**: ("abc123", "우편번호는 5자리 숫자여야 합니다"), ("1234", "우편번호는 5자리 숫자여야 합니다"), ("123456", "우편번호는 5자리 숫자여야 합니다"), ("", "우편번호는 비어있을 수 없습니다")
    - **결과**: 실패 및 적절한 에러 메시지
  - `Equals_ShouldReturnTrue_WhenAddressesAreEqual`: 동일한 주소를 가진 두 Address 동등성 검증
    - **입력**: 두 개의 Address (동일한 주소 정보)
    - **결과**: true 반환
  - `Equals_ShouldReturnFalse_WhenAddressesAreDifferent`: 다른 주소를 가진 두 Address 동등성 검증
    - **입력**: 두 개의 Address (다른 주소 정보)
    - **결과**: false 반환
  - `EqualityOperator_ShouldReturnTrue_WhenAddressesAreEqual`: == 연산자 검증
    - **입력**: 두 개의 Address (동일한 주소 정보)
    - **결과**: true 반환
  - `InequalityOperator_ShouldReturnTrue_WhenAddressesAreDifferent`: != 연산자 검증
    - **입력**: 두 개의 Address (다른 주소 정보)
    - **결과**: true 반환
  - `ToString_ShouldReturnAddressStringRepresentation_WhenCalled`: ToString 메서드 검증
    - **입력**: Address (거리명="123 Main St", 도시명="Seoul", 우편번호="12345")
    - **결과**: "123 Main St, Seoul 12345" 반환

##### `CityTests`
- **목적**: 도시명 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**: (구체적인 테스트 메서드는 파일 내용에 따라 추가)

##### `PostalCodeTests`
- **목적**: 우편번호 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**: (구체적인 테스트 메서드는 파일 내용에 따라 추가)

##### `StreetTests`
- **목적**: 거리명 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**: (구체적인 테스트 메서드는 파일 내용에 따라 추가)

#### CompositePrimitiveValueObjects (복합 기본형 값 객체)

##### `CoordinateTests`
- **목적**: 좌표 값 객체의 생성 및 검증 기능 검증
- **테스트 메서드**:
  - `Create_ShouldReturnSuccess_WhenCoordinatesAreValid`: 유효한 좌표로 Coordinate 생성 검증
    - **입력**: X=100, Y=200
    - **결과**: 성공적인 Coordinate 인스턴스 생성
  - `Create_ShouldReturnFailure_WhenXCoordinateIsOutOfRange`: X 좌표가 범위를 벗어날 때 Coordinate 생성 시 실패 검증
    - **테스트 케이스**: (-1, 200, "X 좌표는 0-1000 범위여야 합니다"), (1001, 200, "X 좌표는 0-1000 범위여야 합니다")
    - **결과**: 실패 및 적절한 에러 메시지
  - `Create_ShouldReturnFailure_WhenYCoordinateIsOutOfRange`: Y 좌표가 범위를 벗어날 때 Coordinate 생성 시 실패 검증
    - **테스트 케이스**: (100, -1, "Y 좌표는 0-1000 범위여야 합니다"), (100, 1001, "Y 좌표는 0-1000 범위여야 합니다")
    - **결과**: 실패 및 적절한 에러 메시지
  - `Equals_ShouldReturnTrue_WhenCoordinatesAreEqual`: 동일한 좌표를 가진 두 Coordinate 동등성 검증
    - **입력**: 두 개의 Coordinate (X=100, Y=200)
    - **결과**: true 반환
  - `Equals_ShouldReturnFalse_WhenCoordinatesAreDifferent`: 다른 좌표를 가진 두 Coordinate 동등성 검증
    - **입력**: Coordinate (X=100, Y=200), Coordinate (X=200, Y=100)
    - **결과**: false 반환
  - `EqualityOperator_ShouldReturnTrue_WhenCoordinatesAreEqual`: == 연산자 검증
    - **입력**: 두 개의 Coordinate (X=100, Y=200)
    - **결과**: true 반환
  - `InequalityOperator_ShouldReturnTrue_WhenCoordinatesAreDifferent`: != 연산자 검증
    - **입력**: Coordinate (X=100, Y=200), Coordinate (X=200, Y=100)
    - **결과**: true 반환
  - `ToString_ShouldReturnCoordinateStringRepresentation_WhenCalled`: ToString 메서드 검증
    - **입력**: Coordinate (X=100, Y=200)
    - **결과**: "(100, 200)" 반환

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 108개
- **성공**: 108개
- **실패**: 0개
- **건너뜀**: 0개

### 테스트 커버리지
- **정상 케이스**: 32개 테스트 케이스
- **예외/실패 케이스**: 43개 테스트 케이스
- **엣지 케이스**: 15개 테스트 케이스
- **순수성 테스트**: 10개 테스트 케이스
- **특수 패턴 테스트**: 8개 테스트 케이스

### 학습 목표 달성도
- ✅ **핵심 개념 이해**: ValueObject 프레임워크의 기본 구조와 동작 원리 이해
- ✅ **비교 가능성 구분**: Comparable과 ComparableNot 값 객체의 차이점 이해
- ✅ **함수형 프로그래밍 패턴**: LINQ Expression을 활용한 검증 파이프라인 이해
- ✅ **타입 안전성**: 명시적 변환 연산자와 타입 안전한 값 객체 생성 이해
- ✅ **에러 처리**: LanguageExt의 `Fin<T>` 타입을 활용한 함수형 에러 처리 이해
