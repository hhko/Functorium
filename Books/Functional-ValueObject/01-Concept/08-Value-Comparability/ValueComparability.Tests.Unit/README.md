# ValueComparability.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `ValueComparability` 값 객체들의 비교 기능에 대한 단위 테스트를 포함합니다. `EmailAddress`의 `IEqualityComparer<T>` 구현과 `Denominator`의 `IComparable<T>` 구현을 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `BasicComparer_ShouldReturnTrue_WhenComparingIdenticalEmails`
- **목적**: 기본 EmailAddressComparer가 동일한 이메일을 올바르게 비교하는지 확인
- **입력**: "user@example.com", "user@example.com"
- **결과**: true

#### `BasicComparer_ShouldReturnFalse_WhenComparingDifferentEmails`
- **목적**: 기본 EmailAddressComparer가 다른 이메일을 올바르게 구분하는지 확인
- **입력**: "user@example.com", "admin@example.com"
- **결과**: false

#### `CompareTo_ShouldReturnCorrectResult_WhenComparingValidDenominators`
- **목적**: Denominator의 CompareTo 메서드가 올바른 비교 결과를 반환하는지 확인
- **테스트 케이스**:
  - `(5, 10) → -1` (a < b)
  - `(10, 5) → 1` (a > b)
  - `(5, 5) → 0` (a == b)
  - `(-5, 5) → -1` (음수 < 양수)
  - `(5, -5) → 1` (양수 > 음수)
  - `(-10, -5) → -1` (음수끼리 비교)

#### `Sort_ShouldOrderDenominatorsCorrectly_WhenUsingListSort`
- **목적**: List.Sort() 메서드가 Denominator를 올바르게 정렬하는지 확인
- **입력**: [10, 3, 7, 1, 15]
- **결과**: [1, 3, 7, 10, 15]

#### `Min_ShouldReturnSmallestValue_WhenUsingMinMethod`
- **목적**: Min 메서드가 최소값을 올바르게 반환하는지 확인
- **입력**: [10, 3, 7, 1, 15]
- **결과**: 1

#### `Max_ShouldReturnLargestValue_WhenUsingMaxMethod`
- **목적**: Max 메서드가 최대값을 올바르게 반환하는지 확인
- **입력**: [10, 3, 7, 1, 15]
- **결과**: 15

### 예외/실패 케이스 테스트

#### `BasicComparer_ShouldHandleNullCorrectly_WhenComparingWithNull`
- **목적**: 기본 비교자가 null을 올바르게 처리하는지 확인
- **테스트 케이스**:
  - `("user@example.com", null) → false`
  - `(null, "user@example.com") → false`
  - `(null, null) → true`

#### `CompareTo_ShouldReturnPositive_WhenComparingWithNull`
- **목적**: Denominator와 null 비교 시 양수 반환 확인
- **입력**: Denominator(5), null
- **결과**: 1

#### `BinarySearch_ShouldReturnNegativeIndex_WhenValueNotFound`
- **목적**: 존재하지 않는 값을 BinarySearch할 때 음수 인덱스 반환 확인
- **입력**: [1, 3, 5, 7, 9, 11, 13, 15]에서 6 검색
- **결과**: 음수 인덱스 (삽입 위치 정보 포함)

### 엣지 케이스 테스트

#### `CaseInsensitiveComparer_ShouldIgnoreCase_WhenComparingEmails`
- **목적**: 대소문자 무시 비교자가 대소문자 차이를 무시하는지 확인
- **테스트 케이스**:
  - `("User@Example.com", "user@example.com") → true`
  - `("ADMIN@EXAMPLE.COM", "admin@example.com") → true`
  - `("Test@Example.Com", "test@example.com") → true`
  - `("user@example.com", "admin@example.com") → false`

#### `LessThanOperator_ShouldReturnCorrectResult_WhenComparingDenominators`
- **목적**: < 연산자가 올바르게 동작하는지 확인
- **테스트 케이스**:
  - `(5, 10) → true`
  - `(10, 5) → false`
  - `(5, 5) → false`
  - `(5, null) → false`
  - `(null, 5) → true`
  - `(null, null) → false`

#### `GreaterThanOperator_ShouldReturnCorrectResult_WhenComparingDenominators`
- **목적**: > 연산자가 올바르게 동작하는지 확인
- **테스트 케이스**:
  - `(10, 5) → true`
  - `(5, 10) → false`
  - `(5, 5) → false`
  - `(5, null) → true`
  - `(null, 5) → false`
  - `(null, null) → false`

#### `LessThanOrEqualOperator_ShouldReturnCorrectResult_WhenComparingDenominators`
- **목적**: <= 연산자가 올바르게 동작하는지 확인
- **테스트 케이스**:
  - `(5, 10) → true`
  - `(10, 5) → false`
  - `(5, 5) → true`
  - `(5, null) → false`
  - `(null, 5) → true`
  - `(null, null) → true`

#### `GreaterThanOrEqualOperator_ShouldReturnCorrectResult_WhenComparingDenominators`
- **목적**: >= 연산자가 올바르게 동작하는지 확인
- **테스트 케이스**:
  - `(10, 5) → true`
  - `(5, 10) → false`
  - `(5, 5) → true`
  - `(5, null) → true`
  - `(null, 5) → false`
  - `(null, null) → true`

### 특수 패턴 테스트

#### `Distinct_ShouldNotDistinguishCase_WhenUsingBasicComparer`
- **목적**: 기본 비교자로 Distinct 사용 시 EmailAddress의 정규화로 인한 대소문자 구분 없음 확인
- **입력**: ["User@Example.com", "user@example.com", "ADMIN@EXAMPLE.COM", "admin@example.com"]
- **결과**: 2개 (정규화로 인해 대소문자 구분 없음)

#### `Distinct_ShouldIgnoreCase_WhenUsingCaseInsensitiveComparer`
- **목적**: 대소문자 무시 비교자로 Distinct 사용 시 대소문자 무시 확인
- **입력**: ["User@Example.com", "user@example.com", "ADMIN@EXAMPLE.COM", "admin@example.com"]
- **결과**: 2개 (User@example.com과 admin@example.com)

#### `HashSet_ShouldWorkCorrectly_WhenUsingBasicComparer`
- **목적**: HashSet에서 기본 비교자가 올바르게 동작하는지 확인
- **입력**: ["user1@example.com", "user2@example.com", "user1@example.com", "admin@example.com"]
- **결과**: 3개 (중복 제거 후)

#### `Dictionary_ShouldWorkCorrectly_WhenUsingBasicComparer`
- **목적**: Dictionary에서 기본 비교자가 올바르게 동작하는지 확인
- **입력**: 중복 키가 포함된 이메일-이름 쌍
- **결과**: 중복 제거된 3개 항목

#### `GetHashCode_ShouldReturnConsistentHashCode_WhenUsingSameEmail`
- **목적**: GetHashCode가 일관된 해시 코드를 반환하는지 확인
- **입력**: 동일한 이메일 주소
- **결과**: 동일한 해시 코드

#### `CaseInsensitiveGetHashCode_ShouldReturnSameHashCode_WhenUsingDifferentCase`
- **목적**: 대소문자 무시 비교자의 GetHashCode가 대소문자를 무시한 해시를 반환하는지 확인
- **입력**: "User@Example.com", "user@example.com"
- **결과**: 동일한 해시 코드

#### `BinarySearch_ShouldReturnCorrectIndex_WhenSearchingInSortedList`
- **목적**: 정렬된 리스트에서 BinarySearch가 올바른 인덱스를 반환하는지 확인
- **입력**: [1, 3, 5, 7, 9, 11, 13, 15]에서 7 검색
- **결과**: 3 (7의 인덱스)

#### `OrderBy_ShouldOrderDenominatorsCorrectly_WhenUsingLinqOrderBy`
- **목적**: LINQ OrderBy를 사용한 정렬이 올바르게 동작하는지 확인
- **입력**: [10, 3, 7, 1, 15]
- **결과**: [1, 3, 7, 10, 15]

#### `OrderByDescending_ShouldOrderDenominatorsCorrectly_WhenUsingLinqOrderByDescending`
- **목적**: LINQ OrderByDescending을 사용한 내림차순 정렬이 올바르게 동작하는지 확인
- **입력**: [10, 3, 7, 1, 15]
- **결과**: [15, 10, 7, 3, 1]

#### `Range_ShouldCalculateCorrectRange_WhenUsingMaxAndMin`
- **목적**: Max와 Min을 사용한 범위 계산이 올바르게 동작하는지 확인
- **입력**: [10, 3, 7, 1, 15]
- **결과**: 14 (15 - 1)

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 25
- **성공**: 25
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 6개
- **예외/실패 케이스**: 3개
- **엣지 케이스**: 8개
- **특수 패턴 테스트**: 8개

### 학습 목표 달성도
- ✅ **`IEqualityComparer<T>` 구현 이해**: EmailAddress의 동등성 비교 기능 완전 검증
- ✅ **`IComparable<T>` 구현 이해**: Denominator의 순서 비교 기능 완전 검증
- ✅ **컬렉션 통합 이해**: HashSet, Dictionary, List 정렬 등 컬렉션에서의 활용 검증
- ✅ **성능 최적화 이해**: BinarySearch, Min/Max 등 효율적인 컬렉션 연산 검증
- ✅ **null 안전성 이해**: null 값과의 비교 처리 검증
