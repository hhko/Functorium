// using ErrorCodeFluent.ValueObjects.Comparable.PrimitiveValueObjects;
// using ErrorCodeFluent.ValueObjects.Comparable.CompositeValueObjects;
// using ErrorCodeFluent.ValueObjects.ComparableNot.CompositeValueObjects;
// using ErrorCodeFluent.ValueObjects.ComparableNot.CompositePrimitiveValueObjects;

// /// <summary>
// /// DomainError 헬퍼와 DomainErrorAssertions를 사용한 타입 안전 테스트 예제
// ///
// /// 테스트 목적:
// /// 1. DomainError.For 메서드가 올바른 에러를 생성하는지 검증
// /// 2. DomainErrorAssertions의 타입 안전 Assertion 메서드 사용법 시연
// /// 3. Fin, Validation 결과에 대한 에러 검증 방법 시연
// /// </summary>
// [Trait("Concept-14-Error-Code-Fluent", "DomainErrorTests")]
// public class DomainErrorTests
// {
//     #region Fin<T> 결과 검증 - ShouldBeDomainError

//     /// <summary>
//     /// Street에서 DomainError.For가 올바른 에러를 생성해야 한다
//     /// </summary>
//     /// <remarks>
//     /// Before (문자열 기반):
//     /// <code>
//     /// result.IsFail.ShouldBeTrue();
//     /// result.IfFail(error => error.Message.ShouldContain("DomainErrors.Street.Empty"));
//     /// </code>
//     ///
//     /// After (타입 안전):
//     /// <code>
//     /// result.ShouldBeDomainError&lt;Street, Street&gt;(new DomainErrorType.Empty());
//     /// </code>
//     /// </remarks>
//     [Fact]
//     public void Street_Create_ShouldReturnDomainError_WhenEmpty()
//     {
//         // Arrange
//         string value = "";

//         // Act
//         var result = Street.Create(value);

//         // Assert - 타입 안전 Assertion (Fin<Street>)
//         result.ShouldBeDomainError<Street, Street>(new DomainErrorType.Empty());
//     }

//     /// <summary>
//     /// City에서 DomainError.For가 올바른 에러를 생성해야 한다
//     /// </summary>
//     [Fact]
//     public void City_Create_ShouldReturnDomainError_WhenEmpty()
//     {
//         // Arrange
//         string value = "";

//         // Act
//         var result = City.Create(value);

//         // Assert - 타입 안전 Assertion (Fin<City>)
//         result.ShouldBeDomainError<City, City>(new DomainErrorType.Empty());
//     }

//     /// <summary>
//     /// PostalCode에서 Empty 에러 검증
//     /// </summary>
//     [Fact]
//     public void PostalCode_Create_ShouldReturnDomainError_WhenEmpty()
//     {
//         // Act
//         var result = PostalCode.Create("");

//         // Assert (Fin<PostalCode>)
//         result.ShouldBeDomainError<PostalCode, PostalCode>(new DomainErrorType.Empty());
//     }

//     /// <summary>
//     /// PostalCode에서 WrongLength 에러 검증
//     /// </summary>
//     [Theory]
//     [InlineData("1234")]      // 4자리
//     [InlineData("123456")]    // 6자리
//     [InlineData("abcde")]     // 문자
//     public void PostalCode_Create_ShouldReturnDomainError_WhenWrongFormat(string value)
//     {
//         // Act
//         var result = PostalCode.Create(value);

//         // Assert (Fin<PostalCode>)
//         result.ShouldBeDomainError<PostalCode, PostalCode>(new DomainErrorType.WrongLength(5));
//     }

//     /// <summary>
//     /// Currency에서 Empty 에러 검증
//     /// </summary>
//     [Fact]
//     public void Currency_Create_ShouldReturnDomainError_WhenEmpty()
//     {
//         // Act
//         var result = Currency.Create("");

//         // Assert (Fin<Currency>)
//         result.ShouldBeDomainError<Currency, Currency>(new DomainErrorType.Empty());
//     }

//     /// <summary>
//     /// Currency에서 WrongLength 에러 검증
//     /// </summary>
//     [Theory]
//     [InlineData("AB")]       // 2자리
//     [InlineData("ABCD")]     // 4자리
//     public void Currency_Create_ShouldReturnDomainError_WhenWrongLength(string value)
//     {
//         // Act
//         var result = Currency.Create(value);

//         // Assert (Fin<Currency>)
//         result.ShouldBeDomainError<Currency, Currency>(new DomainErrorType.WrongLength(3));
//     }

//     /// <summary>
//     /// Currency에서 Custom("Unsupported") 에러 검증
//     /// </summary>
//     [Fact]
//     public void Currency_Create_ShouldReturnDomainError_WhenUnsupported()
//     {
//         // Act
//         var result = Currency.Create("XYZ");

//         // Assert - 도메인 특화 에러 검증 (Fin<Currency>)
//         result.ShouldBeDomainError<Currency, Currency>(new DomainErrorType.Custom("Unsupported"));
//     }

//     /// <summary>
//     /// Coordinate에서 OutOfRange 에러 검증
//     /// </summary>
//     [Theory]
//     [InlineData(-1, 500)]     // X 음수
//     [InlineData(1001, 500)]   // X 초과
//     [InlineData(500, -1)]     // Y 음수
//     [InlineData(500, 1001)]   // Y 초과
//     public void Coordinate_Create_ShouldReturnDomainError_WhenOutOfRange(int x, int y)
//     {
//         // Act
//         var result = Coordinate.Create(x, y);

//         // Assert (Fin<Coordinate>)
//         result.ShouldBeDomainError<Coordinate, Coordinate>(new DomainErrorType.OutOfRange("0", "1000"));
//     }

//     #endregion

//     #region Validation<Error, T> 결과 검증 - ShouldHaveDomainError

//     /// <summary>
//     /// Validation 결과에서 단일 에러 검증
//     /// </summary>
//     [Fact]
//     public void Denominator_Validate_ShouldHaveDomainError_WhenZero()
//     {
//         // Arrange
//         int value = 0;

//         // Act
//         Validation<Error, int> validation = Denominator.Validate(value);

//         // Assert - Validation 결과에 대한 타입 안전 검증
//         validation.ShouldHaveDomainError<Denominator, int>(new DomainErrorType.Custom("Zero"));
//     }

//     /// <summary>
//     /// Validation 결과에서 현재 값까지 검증
//     /// </summary>
//     [Fact]
//     public void Denominator_Validate_ShouldHaveDomainErrorWithValue_WhenZero()
//     {
//         // Arrange
//         int value = 0;

//         // Act
//         Validation<Error, int> validation = Denominator.Validate(value);

//         // Assert - 에러 타입과 현재 값 모두 검증
//         validation.ShouldHaveDomainError<Denominator, int, int>(
//             new DomainErrorType.Custom("Zero"),
//             expectedCurrentValue: 0);
//     }

//     /// <summary>
//     /// Validation 결과에서 정확히 하나의 에러만 있는지 검증
//     /// </summary>
//     [Fact]
//     public void PostalCode_Validate_ShouldHaveOnlyDomainError_WhenEmpty()
//     {
//         // Act
//         Validation<Error, string> validation = PostalCode.Validate("");

//         // Assert - 정확히 1개의 에러만 있는지 검증
//         validation.ShouldHaveOnlyDomainError<PostalCode, string>(new DomainErrorType.Empty());
//     }

//     /// <summary>
//     /// Validation 결과에서 여러 에러 검증 (Apply 패턴 사용 시)
//     /// </summary>
//     [Fact]
//     public void MultipleValidation_ShouldHaveDomainErrors()
//     {
//         // Arrange - 의도적으로 여러 검증 실패 생성
//         Validation<Error, string> emptyError = DomainError.For<PostalCode>(
//             new DomainErrorType.Empty(), "", "Postal code cannot be empty");
//         Validation<Error, string> formatError = DomainError.For<PostalCode>(
//             new DomainErrorType.WrongLength(5), "abc", "Wrong length");

//         // Act - Apply 패턴으로 에러 누적
//         var combined = (emptyError, formatError).Apply((a, b) => a + b).As();

//         // Assert - 여러 에러 모두 포함되어 있는지 검증
//         combined.ShouldHaveDomainErrors<PostalCode, string>(
//             new DomainErrorType.Empty(),
//             new DomainErrorType.WrongLength(5));
//     }

//     #endregion

//     #region 성공 케이스

//     /// <summary>
//     /// 유효한 값으로 값 객체 생성 시 성공해야 한다
//     /// </summary>
//     [Fact]
//     public void ValidValues_ShouldReturnSuccess()
//     {
//         // Act
//         var streetResult = Street.Create("강남대로");
//         var cityResult = City.Create("서울시");
//         var postalCodeResult = PostalCode.Create("12345");

//         // Assert
//         streetResult.IsSucc.ShouldBeTrue();
//         cityResult.IsSucc.ShouldBeTrue();
//         postalCodeResult.IsSucc.ShouldBeTrue();
//     }

//     #endregion
// }
