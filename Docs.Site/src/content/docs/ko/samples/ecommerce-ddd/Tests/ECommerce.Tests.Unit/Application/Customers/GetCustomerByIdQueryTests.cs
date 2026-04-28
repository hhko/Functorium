using ECommerce.Application.Usecases.Customers.Ports;
using ECommerce.Application.Usecases.Customers.Queries;
using ECommerce.Domain.AggregateRoots.Customers;

namespace ECommerce.Tests.Unit.Application.Customers;

public class GetCustomerByIdQueryTests
{
    private readonly ICustomerDetailQuery _adapter = Substitute.For<ICustomerDetailQuery>();
    private readonly GetCustomerByIdQuery.Usecase _sut;

    public GetCustomerByIdQueryTests()
    {
        _sut = new GetCustomerByIdQuery.Usecase(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnCustomer_WhenExists()
    {
        // Arrange
        var customerId = CustomerId.New();
        var dto = new CustomerDetailDto(
            customerId.ToString(), "John", "john@example.com", 5000m,
            DateTime.UtcNow);

        var request = new GetCustomerByIdQuery.Request(customerId.ToString());

        _adapter.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(dto));

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

        _adapter.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Fail<CustomerDetailDto>(Error.New("Customer not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
