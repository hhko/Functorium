using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Customers.Commands;
using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Customers.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Customers;

public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommand.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("", "john@example.com", 5000m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenEmailIsInvalid()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "invalid-email", 5000m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "Email");
    }
}

public class CreateCustomerCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly CreateCustomerCommand.Usecase _sut;

    public CreateCustomerCommandTests()
    {
        _sut = new CreateCustomerCommand.Usecase(_customerRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

        _customerRepository.Exists(Arg.Any<Specification<Customer>>())
            .Returns(FinTFactory.Succ(false));
        _customerRepository.Create(Arg.Any<Customer>())
            .Returns(call => FinTFactory.Succ(call.Arg<Customer>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("John");
        actual.ThrowIfFail().Email.ShouldBe("john@example.com");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateEmail()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

        _customerRepository.Exists(Arg.Any<Specification<Customer>>())
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
