using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

// 테스트용 값 객체
public sealed class LinqTestValueObject : ValueObject
{
    public string Value1 { get; }
    public int Value2 { get; }

    private LinqTestValueObject(string value1, int value2)
    {
        Value1 = value1;
        Value2 = value2;
    }

    public static LinqTestValueObject Create(string value1, int value2) => new(value1, value2);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value1;
        yield return Value2;
    }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class TypedValidationLinqTests
{
    #region SelectMany - TypedValidation to Validation (반환 타입: Validation)

    [Fact]
    public void SelectMany_TypedToValidation_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;

        // Act - 두 번째 from에서 Validation<Error, int>를 반환하므로 결과는 Validation<Error, T>
        Validation<Error, int> secondValidation = ValidationRules<LinqTestValueObject>.Positive(value2);

        Validation<Error, (string, int)> actual =
            from v1 in ValidationRules<LinqTestValueObject>.NotEmpty(value1)
            from v2 in secondValidation
            select (v1, v2);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Item1.ShouldBe(value1);
                v.Item2.ShouldBe(value2);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void SelectMany_TypedToValidation_ReturnsFailure_WhenFirstValidationFails()
    {
        // Arrange
        var value1 = "";
        var value2 = 42;

        // Act
        Validation<Error, int> secondValidation = ValidationRules<LinqTestValueObject>.Positive(value2);

        Validation<Error, (string, int)> actual =
            from v1 in ValidationRules<LinqTestValueObject>.NotEmpty(value1)
            from v2 in secondValidation
            select (v1, v2);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void SelectMany_TypedToValidation_ReturnsFailure_WhenSecondValidationFails()
    {
        // Arrange
        var value1 = "test";
        var value2 = -1;

        // Act
        Validation<Error, int> secondValidation = ValidationRules<LinqTestValueObject>.Positive(value2);

        Validation<Error, (string, int)> actual =
            from v1 in ValidationRules<LinqTestValueObject>.NotEmpty(value1)
            from v2 in secondValidation
            select (v1, v2);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region SelectMany - TypedValidation to TypedValidation (반환 타입: TypedValidation)

    [Fact]
    public void SelectMany_TypedToTyped_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;

        // Act - 두 from 모두 TypedValidation을 반환하면 결과도 TypedValidation
        TypedValidation<LinqTestValueObject, (string, int)> actual =
            from v1 in ValidationRules<LinqTestValueObject>.NotEmpty(value1)
            from v2 in ValidationRules<LinqTestValueObject>.Positive(value2)
            select (v1, v2);

        // Assert - TypedValidation이므로 .Value로 접근
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void SelectMany_TypedToTyped_ShortCircuits_OnFirstFailure()
    {
        // Arrange
        var value1 = ""; // Will fail
        var value2 = 42;

        // Act
        TypedValidation<LinqTestValueObject, (string, int)> actual =
            from v1 in ValidationRules<LinqTestValueObject>.NotEmpty(value1)
            from v2 in ValidationRules<LinqTestValueObject>.Positive(value2)
            select (v1, v2);

        // Assert - TypedValidation이므로 .Value로 접근
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(1)); // Only first error
    }

    #endregion

    #region Select

    [Fact]
    public void Select_TransformsValue_WhenValidationSucceeds()
    {
        // Arrange
        var value = "test";

        // Act
        var actual = ValidationRules<LinqTestValueObject>.NotEmpty(value)
            .Select(v => v.ToUpperInvariant());

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe("TEST"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Select_PreservesFailure_WhenValidationFails()
    {
        // Arrange
        var value = "";

        // Act
        var actual = ValidationRules<LinqTestValueObject>.NotEmpty(value)
            .Select(v => v.ToUpperInvariant());

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
    }

    #endregion

    #region ToValidation

    [Fact]
    public void ToValidation_ConvertsTypedValidation_ToValidation()
    {
        // Arrange
        var value = "test";

        // Act
        Validation<Error, string> actual = ValidationRules<LinqTestValueObject>.NotEmpty(value)
            .ToValidation();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v => v.ShouldBe(value),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ToValidation_PreservesFailure_WhenValidationFails()
    {
        // Arrange
        var value = "";

        // Act
        Validation<Error, string> actual = ValidationRules<LinqTestValueObject>.NotEmpty(value)
            .ToValidation();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region Complex LINQ Scenarios

    [Fact]
    public void LinqQuery_WithThreeValidations_WorksWithoutCasting()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-1);
        var endDate = DateTime.Now.AddDays(1);
        var name = "Test";

        // Act - 모든 from이 TypedValidation이면 결과도 TypedValidation
        TypedValidation<LinqTestValueObject, (DateTime, DateTime, string)> actual =
            from validStart in ValidationRules<LinqTestValueObject>.NotDefault(startDate)
            from validEnd in ValidationRules<LinqTestValueObject>.NotDefault(endDate)
            from validName in ValidationRules<LinqTestValueObject>.NotEmpty(name)
            select (validStart, validEnd, validName);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void LinqQuery_WithMixedValidations_WorksCorrectly()
    {
        // Arrange
        var value = "test";
        var number = 100;

        // Act - TypedValidation에서 Validation으로 체이닝하면 결과는 Validation
        var typedValidation = ValidationRules<LinqTestValueObject>.NotEmpty(value);
        Validation<Error, int> directValidation = ValidationRules<LinqTestValueObject>.Between(number, 0, 200);

        Validation<Error, LinqTestValueObject> actual =
            from v1 in typedValidation
            from v2 in directValidation
            select LinqTestValueObject.Create(v1, v2);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
