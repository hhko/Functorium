using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class ValidationApplyExtensionsTests
{
    #region 2-Tuple Apply

    [Fact]
    public void Apply_2Tuple_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;

        Validation<Error, string> v1 = value1;
        Validation<Error, int> v2 = value2;

        // Act - No .As() needed
        Validation<Error, (string, int)> actual = (v1, v2)
            .Apply((a, b) => (a, b));

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
    public void Apply_2Tuple_CollectsAllErrors_WhenBothFail()
    {
        // Arrange
        Validation<Error, string> v1 = Error.New("Error 1");
        Validation<Error, int> v2 = Error.New("Error 2");

        // Act
        var actual = (v1, v2).Apply((a, b) => (a, b));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(2));
    }

    [Fact]
    public void Apply_2Tuple_ReturnsFailure_WhenFirstFails()
    {
        // Arrange
        Validation<Error, string> v1 = Error.New("Error 1");
        Validation<Error, int> v2 = 42;

        // Act
        var actual = (v1, v2).Apply((a, b) => (a, b));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(1));
    }

    [Fact]
    public void Apply_2Tuple_ReturnsFailure_WhenSecondFails()
    {
        // Arrange
        Validation<Error, string> v1 = "test";
        Validation<Error, int> v2 = Error.New("Error 2");

        // Act
        var actual = (v1, v2).Apply((a, b) => (a, b));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(1));
    }

    #endregion

    #region 3-Tuple Apply

    [Fact]
    public void Apply_3Tuple_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        Validation<Error, string> v1 = "test";
        Validation<Error, int> v2 = 42;
        Validation<Error, decimal> v3 = 100.5m;

        // Act - No .As() needed
        Validation<Error, (string, int, decimal)> actual = (v1, v2, v3)
            .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Item1.ShouldBe("test");
                v.Item2.ShouldBe(42);
                v.Item3.ShouldBe(100.5m);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_3Tuple_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        Validation<Error, string> v1 = Error.New("Error 1");
        Validation<Error, int> v2 = Error.New("Error 2");
        Validation<Error, decimal> v3 = Error.New("Error 3");

        // Act
        var actual = (v1, v2, v3).Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(3));
    }

    #endregion

    #region 4-Tuple Apply

    [Fact]
    public void Apply_4Tuple_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        Validation<Error, string> v1 = "test";
        Validation<Error, int> v2 = 42;
        Validation<Error, decimal> v3 = 100.5m;
        Validation<Error, string> v4 = "extra";

        // Act - No .As() needed
        Validation<Error, (string, int, decimal, string)> actual = (v1, v2, v3, v4)
            .Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Item1.ShouldBe("test");
                v.Item2.ShouldBe(42);
                v.Item3.ShouldBe(100.5m);
                v.Item4.ShouldBe("extra");
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_4Tuple_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        Validation<Error, string> v1 = Error.New("Error 1");
        Validation<Error, int> v2 = Error.New("Error 2");
        Validation<Error, decimal> v3 = Error.New("Error 3");
        Validation<Error, string> v4 = Error.New("Error 4");

        // Act
        var actual = (v1, v2, v3, v4).Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(4));
    }

    #endregion

    #region 5-Tuple Apply

    [Fact]
    public void Apply_5Tuple_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        Validation<Error, string> v1 = "test";
        Validation<Error, int> v2 = 42;
        Validation<Error, decimal> v3 = 100.5m;
        Validation<Error, string> v4 = "extra";
        Validation<Error, bool> v5 = true;

        // Act - No .As() needed
        Validation<Error, (string, int, decimal, string, bool)> actual = (v1, v2, v3, v4, v5)
            .Apply((a, b, c, d, e) => (a, b, c, d, e));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Item1.ShouldBe("test");
                v.Item2.ShouldBe(42);
                v.Item3.ShouldBe(100.5m);
                v.Item4.ShouldBe("extra");
                v.Item5.ShouldBeTrue();
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_5Tuple_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        Validation<Error, string> v1 = Error.New("Error 1");
        Validation<Error, int> v2 = Error.New("Error 2");
        Validation<Error, decimal> v3 = Error.New("Error 3");
        Validation<Error, string> v4 = Error.New("Error 4");
        Validation<Error, bool> v5 = Error.New("Error 5");

        // Act
        var actual = (v1, v2, v3, v4, v5).Apply((a, b, c, d, e) => (a, b, c, d, e));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(5));
    }

    #endregion

    #region Real-World Pattern Tests

    [Fact]
    public void Apply_MoneyPattern_WorksWithoutAs()
    {
        // Arrange - Simulating Money.Validate pattern with explicit Validation<Error, T> methods
        var amount = 100m;
        var currency = "USD";

        static Validation<Error, decimal> ValidateAmount(decimal amount) =>
            amount >= 0
                ? amount
                : DomainError.For<ValidationApplyExtensionsTests, decimal>(
                    new DomainErrorType.Negative(), amount, "Amount must be non-negative");

        static Validation<Error, string> ValidateCurrency(string currency) =>
            !string.IsNullOrEmpty(currency) && currency.Length == 3
                ? currency.ToUpperInvariant()
                : DomainError.For<ValidationApplyExtensionsTests>(
                    new DomainErrorType.WrongLength(3), currency, "Currency must be 3 characters");

        // Act - No .As() needed
        var actual = (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (Amount: a, Currency: c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Amount.ShouldBe(amount);
                v.Currency.ShouldBe("USD");
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_UserRegistrationPattern_WorksWithoutAs()
    {
        // Arrange - Simulating UserRegistration pattern
        var email = "user@example.com";
        var password = "password123";
        var name = "John";
        var age = 25;

        static Validation<Error, string> ValidateEmail(string email) =>
            email.Contains("@") ? email : Error.New("Invalid email");

        static Validation<Error, string> ValidatePassword(string password) =>
            password.Length >= 8 ? password : Error.New("Password too short");

        static Validation<Error, string> ValidateName(string name) =>
            !string.IsNullOrEmpty(name) ? name : Error.New("Name required");

        static Validation<Error, int> ValidateAge(int age) =>
            age >= 0 && age <= 150 ? age : Error.New("Invalid age");

        // Act - No .As() needed
        var actual = (ValidateEmail(email), ValidatePassword(password), ValidateName(name), ValidateAge(age))
            .Apply((e, p, n, a) => (Email: e, Password: p, Name: n, Age: a));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Email.ShouldBe(email);
                v.Password.ShouldBe(password);
                v.Name.ShouldBe(name);
                v.Age.ShouldBe(age);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    #endregion
}
