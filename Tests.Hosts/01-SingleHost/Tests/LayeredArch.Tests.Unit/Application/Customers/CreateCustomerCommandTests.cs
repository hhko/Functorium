using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using LayeredArch.Application.Usecases.Customers;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Customers;

public class CreateCustomerCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly CreateCustomerCommand.Usecase _sut;

    public CreateCustomerCommandTests()
    {
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _sut = new CreateCustomerCommand.Usecase(_customerRepository, _unitOfWork, _eventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

        _customerRepository.ExistsByEmail(Arg.Any<Email>())
            .Returns(FinTFactory.Succ(false));
        _customerRepository.Create(Arg.Any<Customer>())
            .Returns(call => FinTFactory.Succ(call.Arg<Customer>()));
        _eventPublisher.PublishEvents(Arg.Any<Customer>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("John");
        actual.ThrowIfFail().Email.ShouldBe("john@example.com");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("", "john@example.com", 5000m);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailIsInvalid()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "invalid-email", 5000m);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateEmail()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

        _customerRepository.ExistsByEmail(Arg.Any<Email>())
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesBeforePublishEvents_WhenRequestIsValid()
    {
        // Arrange
        var callOrder = new List<string>();
        var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

        _customerRepository.ExistsByEmail(Arg.Any<Email>())
            .Returns(FinTFactory.Succ(false));
        _customerRepository.Create(Arg.Any<Customer>())
            .Returns(call => FinTFactory.Succ(call.Arg<Customer>()));
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("SaveChanges"));
        _eventPublisher.PublishEvents(Arg.Any<Customer>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("PublishEvents"));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        callOrder.ShouldBe(["SaveChanges", "PublishEvents"]);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSaveChanges_WhenValidationFails()
    {
        // Arrange
        var request = new CreateCustomerCommand.Request("", "john@example.com", 5000m);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
        _unitOfWork.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
    }
}
