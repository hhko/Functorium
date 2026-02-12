using LayeredArch.Application.Usecases.Customers;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Customers;

public class GetCustomerByIdQueryTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly GetCustomerByIdQuery.Usecase _sut;

    public GetCustomerByIdQueryTests()
    {
        _sut = new GetCustomerByIdQuery.Usecase(_customerRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnCustomer_WhenExists()
    {
        // Arrange
        var customer = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());

        var request = new GetCustomerByIdQuery.Request(customer.Id.ToString());

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("John");
        actual.ThrowIfFail().Email.ShouldBe("john@example.com");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var request = new GetCustomerByIdQuery.Request(CustomerId.New().ToString());

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Fail<Customer>(Error.New("Customer not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
