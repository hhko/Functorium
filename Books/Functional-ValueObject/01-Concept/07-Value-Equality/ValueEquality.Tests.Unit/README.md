# ValueEquality.Tests.Unit

## 목차
- [개요](#개요)
- [테스트 시나리오](#테스트-시나리오)
- [테스트 결과](#테스트-결과)

## 개요

이 프로젝트는 `Denominator` 값 객체의 **값 기반 동등성(Value Equality)** 구현에 대한 단위 테스트를 포함합니다. `IEquatable<T>` 인터페이스, `GetHashCode`와 `Equals`의 일관성, 연산자 오버로딩, 컬렉션에서의 동등성 동작, 성능 최적화를 모두 테스트합니다.

## 테스트 시나리오

### 정상 케이스 테스트

#### `Equals_ShouldReturnTrue_WhenValuesAreEqual`
- **목적**: 같은 값을 가진 두 Denominator 객체는 값 기반으로 동등해야 한다
- **입력**: a = Denominator.Create(5), b = Denominator.Create(5)
- **결과**: a.Equals(b) = true, (a == b) = true, (a != b) = false

#### `Equals_ShouldReturnFalse_WhenValuesAreDifferent`
- **목적**: 다른 값을 가진 두 Denominator 객체는 동등하지 않아야 한다
- **입력**: a = Denominator.Create(5), b = Denominator.Create(10)
- **결과**: a.Equals(b) = false, (a == b) = false, (a != b) = true

#### `GetHashCode_ShouldReturnSameHashCode_WhenValuesAreEqual`
- **목적**: 같은 값을 가진 두 Denominator 객체는 같은 해시 코드를 가져야 한다
- **입력**: a = Denominator.Create(5), b = Denominator.Create(5)
- **결과**: a.GetHashCode() == b.GetHashCode() = true

#### `GetHashCode_ShouldReturnDifferentHashCode_WhenValuesAreDifferent`
- **목적**: 다른 값을 가진 두 Denominator 객체는 다른 해시 코드를 가져야 한다
- **입력**: a = Denominator.Create(5), b = Denominator.Create(10)
- **결과**: a.GetHashCode() != b.GetHashCode() = true

#### `ReferenceEquals_ShouldReturnFalse_WhenValuesAreEqual`
- **목적**: 참조 동등성과 값 동등성의 차이를 확인해야 한다
- **입력**: a = Denominator.Create(5), b = Denominator.Create(5)
- **결과**: ReferenceEquals(a, b) = false, a.Equals(b) = true

### 엣지 케이스 테스트

#### `Equals_ShouldReturnFalse_WhenComparingWithNull`
- **목적**: null과의 비교에서 false를 반환해야 한다
- **입력**: a = Denominator.Create(5), null
- **결과**: a.Equals(null) = false, (a == null) = false, (null == a) = false

### 특수 패턴 테스트

#### `HashSet_ShouldRemoveDuplicates_WhenUsingValueEquality`
- **목적**: HashSet에서 값 기반 동등성을 사용하여 중복 제거가 올바르게 동작해야 한다
- **입력**: [5, 10, 5, 15, 10] → Denominator 배열
- **결과**: HashSet.Count = 3 (중복 제거), 5, 10, 15 포함

#### `Dictionary_ShouldFindKey_WhenUsingValueEquality`
- **목적**: Dictionary에서 값 기반 동등성을 사용하여 키 검색이 올바르게 동작해야 한다
- **입력**: Dictionary<Denominator, string> with keys [5, 10, 15], search key = 5
- **결과**: dictionary.TryGetValue(key) = "Value_5"

#### `LinqExpression_ShouldWorkCorrectly_WhenCreatingMultipleDenominators`
- **목적**: LINQ 표현식을 사용하여 여러 Denominator 생성과 비교가 올바르게 동작해야 한다
- **입력**: from a in Denominator.Create(5) from b in Denominator.Create(5) from c in Denominator.Create(10)
- **결과**: (a == b) = true, (a == c) = false, a.Equals(b) = true, a.Equals(c) = false

#### `Performance_ShouldBeBetter_WhenUsingIEquatable`
- **목적**: `IEquatable<T>`와 Object.Equals의 성능 차이를 확인해야 한다
- **입력**: 100,000회 반복 비교
- **결과**: `IEquatable<T>` 사용 시간 ≤ Object.Equals 사용 시간

#### `GetHashCode_ShouldBeConsistent_WhenValuesAreEqual`
- **목적**: 해시 코드가 일관되게 동작해야 한다
- **테스트 케이스**:
  - `(5, 5) → a.GetHashCode() == b.GetHashCode()`
  - `(10, 10) → a.GetHashCode() == b.GetHashCode()`
  - `(-5, -5) → a.GetHashCode() == b.GetHashCode()`

## 테스트 결과

### 테스트 케이스 통계
- **총 테스트 수**: 13
- **성공**: 13
- **실패**: 0
- **건너뜀**: 0

### 테스트 커버리지
- **정상 케이스**: 5개 (값 기반 동등성, 해시 코드 일관성, 참조 vs 값 동등성)
- **엣지 케이스**: 1개 (null 비교)
- **특수 패턴 테스트**: 7개 (컬렉션 동작, LINQ 표현식, 성능 테스트, Theory 테스트)

### 학습 목표 달성도
- ✅ **값 기반 동등성 이해**: `IEquatable<T>` 인터페이스 구현과 값 기반 비교 동작 확인
- ✅ **참조 vs 값 동등성 구분**: `ReferenceEquals`와 `Equals`의 차이점 명확히 이해
- ✅ **GetHashCode와 Equals 일관성**: 해시 기반 컬렉션에서의 올바른 동작 보장
- ✅ **컬렉션에서의 동등성 활용**: `HashSet<T>`, `Dictionary<TKey, TValue>`에서의 올바른 동작 확인
- ✅ **성능 최적화 이해**: `IEquatable<T>` 사용 시 박싱/언박싱 오버헤드 방지 효과 확인
- ✅ **LINQ 표현식 활용**: 모나드 체이닝을 통한 함수형 프로그래밍 패턴 적용

