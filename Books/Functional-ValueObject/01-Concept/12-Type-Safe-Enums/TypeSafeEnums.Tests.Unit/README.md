# TypeSafeEnums.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `TypeSafeEnums` 프로젝트의 `Currency`, `Price`, `PriceRange` 값 객체에 대한 단위 테스트를 포함합니다. SmartEnum 패키지를 활용한 타입 안전한 열거형 구현과 복합 값 객체의 검증, 비교 기능을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### CurrencyTests - SmartEnum 기반 통화 테스트

#### `Create_ShouldReturnSuccessResult_WhenValidCurrencyCode`
- **목적**: 유효한 통화 코드로 Currency 인스턴스 생성 확인
- **입력**: currencyCode = "KRW"
- **결과**: Currency.KRW 인스턴스 반환

#### `Create_ShouldReturnSuccessResult_WhenSupportedCurrencyCode`
- **목적**: 지원되는 모든 통화 코드로 Currency 인스턴스 생성 확인
- **테스트 케이스**:
  - `("KRW", "한국 원화", "₩")`
  - `("USD", "미국 달러", "$")`
  - `("EUR", "유로", "€")`
  - `("JPY", "일본 엔", "¥")`
  - `("CNY", "중국 위안", "¥")`
  - `("GBP", "영국 파운드", "£")`
  - `("AUD", "호주 달러", "A$")`
  - `("CAD", "캐나다 달러", "C$")`
  - `("CHF", "스위스 프랑", "CHF")`
  - `("SGD", "싱가포르 달러", "S$")`

#### `Create_ShouldReturnSuccessResult_WhenLowerCaseCurrencyCode`
- **목적**: 소문자 통화 코드 입력 시 대문자로 정규화되어 생성 확인
- **테스트 케이스**:
  - `("krw", "KRW")`
  - `("eur", "EUR")`
  - `("usd", "USD")`

#### `CreateFromValidated_ShouldReturnCurrencyInstance_WhenValidatedCurrencyCode`
- **목적**: 검증된 통화 코드로 Currency 인스턴스 생성 확인
- **테스트 케이스**:
  - `("KRW")`
  - `("USD")`
  - `("EUR")`

#### `GetAllSupportedCurrencies_ShouldReturnAllSupportedCurrencies_WhenCalled`
- **목적**: 지원되는 모든 통화 목록 반환 확인
- **결과**: 10개 통화 코드 반환

#### `ToString_ShouldReturnFormattedCurrencyInfo_WhenCalled`
- **목적**: 통화 정보의 올바른 문자열 형식 반환 확인
- **테스트 케이스**:
  - `("USD", "USD (미국 달러) $")`
  - `("EUR", "EUR (유로) €")`
  - `("KRW", "KRW (한국 원화) ₩")`

#### `FormatAmount_ShouldReturnFormattedAmount_WhenValidAmount`
- **목적**: 금액 포맷팅 기능 정상 동작 확인
- **입력**: 1234.56m
- **결과**: "1,234.56" 형식으로 포맷팅

#### `FormatAmountWithoutDecimals_ShouldReturnFormattedAmountWithoutDecimals_WhenValidAmount`
- **목적**: 소수점 없는 금액 포맷팅 기능 정상 동작 확인
- **입력**: 1234.56m
- **결과**: "1,235" 형식으로 포맷팅

#### `StaticFields_ShouldBeCorrectlyDefined_WhenAccessed`
- **목적**: 정적 필드들이 올바르게 정의되어 접근 가능한지 확인
- **결과**: 모든 정적 통화 필드 정상 접근

#### `FromValue_ShouldReturnCorrectCurrency_WhenValidValue`
- **목적**: FromValue 메서드로 올바른 통화 반환 확인
- **테스트 케이스**:
  - `("USD", "USD")`
  - `("EUR", "EUR")`
  - `("KRW", "KRW")`

#### `TryFromValue_ShouldReturnCorrectResult_WhenVariousValues`
- **목적**: TryFromValue 메서드의 성공/실패 결과 확인
- **테스트 케이스**:
  - `("KRW", true)`
  - `("INVALID", false)`
  - `("USD", true)`

#### PriceTests - 복합 값 객체 테스트

#### `Create_ShouldReturnSuccessResult_WhenValidPriceValueAndCurrency`
- **목적**: 유효한 가격 값과 통화 코드로 Price 인스턴스 생성 확인
- **테스트 케이스**:
  - `(0, "KRW")`
  - `(100, "USD")`
  - `(1000.50, "EUR")`
  - `(999999.99, "KRW")`

#### `CreateFromValidated_ShouldReturnPriceInstance_WhenValidatedPriceValue`
- **목적**: 검증된 값으로 Price 인스턴스 생성 확인
- **테스트 케이스**:
  - `(0, "KRW")`
  - `(100, "USD")`
  - `(1000.50, "EUR")`

#### `ToString_ShouldReturnFormattedPriceInfo_WhenCalled`
- **목적**: 가격 정보의 올바른 문자열 형식 반환 확인
- **테스트 케이스**:
  - `(0, "KRW", "KRW (한국 원화) ₩ 0.00")`
  - `(1000, "USD", "USD (미국 달러) $ 1,000.00")`
  - `(12345, "EUR", "EUR (유로) € 12,345.00")`

#### `CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingSameCurrencyPrices`
- **목적**: 같은 통화의 Price 인스턴스들 비교 결과 확인
- **테스트 케이스**:
  - `(100, 200, "KRW", -1)` - 100 < 200
  - `(200, 100, "USD", 1)` - 200 > 100
  - `(100, 100, "EUR", 0)` - 100 == 100

#### `Equals_ShouldReturnCorrectEqualityResult_WhenComparingPrices`
- **목적**: Price 인스턴스들의 동등성 비교 결과 확인
- **테스트 케이스**:
  - `(100, 100, "KRW", true)`
  - `(100, 200, "USD", false)`
  - `(0, 0, "EUR", true)`

#### `GetHashCode_ShouldReturnSameHashCode_WhenSamePriceValue`
- **목적**: 같은 가격 값의 해시 코드 일치 확인
- **입력**: 100, "KRW"
- **결과**: 동일한 해시 코드 반환

#### `ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPrices`
- **목적**: Price 인스턴스들의 비교 연산자 결과 확인
- **테스트 케이스**:
  - `(100, 200, "KRW", true, false, false, true, false, true)` - 100 < 200
  - `(200, 100, "USD", false, true, false, false, true, true)` - 200 > 100
  - `(100, 100, "EUR", false, false, true, true, true, false)` - 100 == 100

#### `ComparisonWithNull_ShouldReturnCorrectResults_WhenComparingWithNull`
- **목적**: Price 인스턴스와 null 비교 결과 확인
- **결과**: null과의 비교에서 올바른 결과 반환

#### `Equals_ShouldReturnFalse_WhenComparingWithDifferentType`
- **목적**: Price 인스턴스와 다른 타입 비교 시 false 반환 확인
- **입력**: Price 인스턴스와 문자열
- **결과**: false 반환

#### `AmountProperty_ShouldReturnDecimalValue_WhenPriceInstance`
- **목적**: Price 인스턴스의 Amount 속성을 통한 decimal 값 접근 확인
- **테스트 케이스**:
  - `(0, "KRW")`
  - `(100, "USD")`
  - `(1000.50, "EUR")`

#### `CurrencyProperty_ShouldReturnCurrencyInfo_WhenPriceInstance`
- **목적**: Price 인스턴스의 Currency 속성을 통한 통화 정보 접근 확인
- **테스트 케이스**:
  - `(100, "KRW")`
  - `(200, "USD")`
  - `(300, "EUR")`

#### PriceRangeTests - 복합 값 객체 범위 테스트

#### `Create_ShouldReturnSuccessResult_WhenValidPriceRange`
- **목적**: 유효한 가격 범위로 PriceRange 인스턴스 생성 확인
- **테스트 케이스**:
  - `(10000, 50000, "KRW")`
  - `(100, 500, "USD")`
  - `(80, 400, "EUR")`

#### `CreateFromValidated_ShouldReturnPriceRangeInstance_WhenValidatedValueObjects`
- **목적**: 검증된 값 객체들로 PriceRange 인스턴스 생성 확인
- **테스트 케이스**:
  - `(10000, 50000, "KRW")`
  - `(100, 500, "USD")`
  - `(80, 400, "EUR")`

#### `ToString_ShouldReturnFormattedPriceRangeInfo_WhenCalled`
- **목적**: 가격 범위 정보의 올바른 문자열 형식 반환 확인
- **테스트 케이스**:
  - `(10000, 50000, "KRW", "KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 50,000.00")`
  - `(100, 500, "USD", "USD (미국 달러) $ 100.00 ~ USD (미국 달러) $ 500.00")`
  - `(80, 400, "EUR", "EUR (유로) € 80.00 ~ EUR (유로) € 400.00")`

#### `CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingPriceRanges`
- **목적**: PriceRange 인스턴스들의 비교 결과 확인
- **테스트 케이스**:
  - `(10000, 30000, "KRW", 20000, 40000, "KRW", -1)` - 첫 번째가 더 작음
  - `(20000, 40000, "KRW", 10000, 30000, "KRW", 1)` - 첫 번째가 더 큼
  - `(10000, 30000, "KRW", 10000, 30000, "KRW", 0)` - 같음

#### `Equals_ShouldReturnCorrectEqualityResult_WhenComparingPriceRanges`
- **목적**: PriceRange 인스턴스들의 동등성 비교 결과 확인
- **테스트 케이스**:
  - `(10000, 30000, "KRW", 10000, 30000, "KRW", true)`
  - `(10000, 30000, "KRW", 20000, 40000, "KRW", false)`
  - `(10000, 30000, "KRW", 10000, 30000, "USD", false)`

#### `ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPriceRanges`
- **목적**: PriceRange 인스턴스들의 비교 연산자 결과 확인
- **테스트 케이스**:
  - `(10000, 30000, "KRW", 20000, 40000, "KRW", true, false, false, true, false, true)` - 첫 번째가 더 작음
  - `(20000, 40000, "KRW", 10000, 30000, "KRW", false, true, false, false, true, true)` - 첫 번째가 더 큼
  - `(10000, 30000, "KRW", 10000, 30000, "KRW", false, false, true, true, true, false)` - 같음

#### `ComparisonWithNull_ShouldReturnCorrectResults_WhenComparingWithNull`
- **목적**: PriceRange 인스턴스와 null 비교 결과 확인
- **결과**: null과의 비교에서 올바른 결과 반환

#### `Equals_ShouldReturnFalse_WhenComparingWithDifferentType`
- **목적**: PriceRange 인스턴스와 다른 타입 비교 시 false 반환 확인
- **입력**: PriceRange 인스턴스와 문자열
- **결과**: false 반환

#### `GetHashCode_ShouldReturnSameHashCode_WhenSamePriceRange`
- **목적**: 같은 가격 범위의 해시 코드 일치 확인
- **입력**: (10000, 50000, "KRW")
- **결과**: 동일한 해시 코드 반환

#### `Properties_ShouldBeAccessible_WhenPriceRangeCreated`
- **목적**: PriceRange 인스턴스의 속성들 접근 가능 확인
- **테스트 케이스**:
  - `(10000, 50000, "KRW")`
  - `(100, 500, "USD")`
  - `(80, 400, "EUR")`

#### `Sort_ShouldOrderCorrectly_WhenSameCurrencyPriceRanges`
- **목적**: 같은 통화의 PriceRange들이 올바르게 정렬되는지 확인
- **입력**: 3개의 PriceRange 배열
- **결과**: 최소 가격 기준으로 오름차순 정렬

### 예외/실패 케이스 테스트

#### CurrencyTests

#### `Create_ShouldReturnFailureResult_WhenEmptyCurrencyCode`
- **목적**: 빈 통화 코드로 Currency 생성 시 실패 확인
- **입력**: currencyCode = "" 또는 "   "
- **결과**: 실패 결과 반환

#### `Create_ShouldReturnFailureResult_WhenNullCurrencyCode`
- **목적**: null 통화 코드로 Currency 생성 시 실패 확인
- **입력**: currencyCode = null
- **결과**: 실패 결과 반환

#### `Create_ShouldReturnFailureResult_WhenInvalidFormatCurrencyCode`
- **목적**: 잘못된 형식의 통화 코드로 Currency 생성 시 실패 확인
- **테스트 케이스**:
  - `("KR")` - 2자리
  - `("123")` - 숫자
  - `("KR1")` - 숫자 포함
  - `("K-R")` - 하이픈 포함
  - `("KOREA")` - 5자리

#### `Create_ShouldReturnFailureResult_WhenUnsupportedCurrencyCode`
- **목적**: 지원하지 않는 3자리 통화 코드로 Currency 생성 시 실패 확인
- **테스트 케이스**:
  - `("abc")`
  - `("ABC")`
  - `("XYZ")`

#### `Validate_ShouldReturnCorrectValidationResult_WhenVariousCurrencyCodes`
- **목적**: 다양한 통화 코드에 대한 검증 결과 확인
- **테스트 케이스**:
  - `("KR", false)`
  - `("USD", true)`
  - `("KRW", true)`
  - `("", false)`
  - `("INVALID", false)`

#### PriceTests

#### `Create_ShouldReturnFailureResult_WhenNegativePriceValue`
- **목적**: 음수 가격 값으로 Price 생성 시 실패 확인
- **테스트 케이스**:
  - `(-1, "KRW")`
  - `(-100, "USD")`
  - `(-0.01, "EUR")`

#### `Validate_ShouldReturnCorrectValidationResult_WhenVariousPriceValues`
- **목적**: 다양한 가격 값에 대한 검증 결과 확인
- **테스트 케이스**:
  - `(0, "KRW", true)`
  - `(100, "USD", true)`
  - `(-1, "EUR", false)`
  - `(-100, "KRW", false)`

#### PriceRangeTests

#### `Create_ShouldReturnFailureResult_WhenMinPriceGreaterThanMaxPrice`
- **목적**: 최소 가격이 최대 가격보다 클 때 PriceRange 생성 시 실패 확인
- **테스트 케이스**:
  - `(50000, 10000, "KRW")`
  - `(500, 100, "USD")`
  - `(400, 80, "EUR")`

#### `Create_ShouldReturnFailureResult_WhenNegativePrice`
- **목적**: 음수 가격으로 PriceRange 생성 시 실패 확인
- **테스트 케이스**:
  - `(-1000, -5000, "KRW")`
  - `(-1000, 50000, "KRW")`
  - `(10000, -5000, "KRW")`

#### `Create_ShouldReturnFailureResult_WhenUnsupportedCurrencyCode`
- **목적**: 지원하지 않는 통화 코드로 PriceRange 생성 시 실패 확인
- **테스트 케이스**:
  - `("ABC")`
  - `("abc")`
  - `("XYZ")`

#### `Validate_ShouldReturnCorrectValidationResult_WhenVariousInputs`
- **목적**: 다양한 입력에 대한 검증 결과 확인
- **테스트 케이스**:
  - `(10000, 50000, "KRW", true)`
  - `(50000, 10000, "KRW", false)`
  - `(-1000, 50000, "KRW", false)`
  - `(10000, 50000, "INVALID", false)`

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 119
- **성공**: 119
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 75개 테스트 케이스
- **예외/실패 케이스**: 44개 테스트 케이스
- **엣지 케이스**: 0개 테스트 케이스
- **순수성 테스트**: 0개 테스트 케이스
- **특수 패턴 테스트**: 0개 테스트 케이스

### 학습 목표 달성도
- ✅ **SmartEnum 패키지 활용**: Ardalis.SmartEnum을 활용한 타입 안전한 열거형 구현 검증
- ✅ **복합 값 객체 구현**: MoneyAmount와 Currency를 조합한 Price, PriceRange 구현 검증
- ✅ **LINQ Expression 활용**: 함수형 프로그래밍 스타일의 검증 로직 구현 검증
- ✅ **비교 기능 구현**: ComparableValueObject 기반의 비교 연산자 및 정렬 기능 검증
- ✅ **도메인 규칙 적용**: 통화별 가격 비교, 범위 검증 등 비즈니스 규칙 적용 검증
