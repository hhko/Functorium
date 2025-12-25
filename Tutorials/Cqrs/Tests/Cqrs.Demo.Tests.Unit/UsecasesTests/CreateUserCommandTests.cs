using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Cqrs.Demo.Tests.Unit.UsecasesTests;

public sealed class CreateUserCommandTests
{
    private readonly ILogger<CreateUserCommand.Usecase> _logger;
    private readonly IUserRepository _userRepository;
    private readonly CreateUserCommand.Usecase _sut;

    public CreateUserCommandTests()
    {
        _logger = Substitute.For<ILogger<CreateUserCommand.Usecase>>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new CreateUserCommand.Usecase(_logger, _userRepository);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new CreateUserCommand.Request("Alice", "alice@example.com");
        var expectedUser = new User(Guid.NewGuid(), "Alice", "alice@example.com", DateTime.UtcNow);

        _userRepository
            .ExistsByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(false)));

        _userRepository
            .CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var user = callInfo.Arg<User>();
                return Task.FromResult(Fin.Succ(user));
            });

        // Act
        CreateUserCommand.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alice");
        result.Email.ShouldBe("alice@example.com");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserCommand.Request("Alice", "existing@example.com");

        _userRepository
            .ExistsByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(true)));

        // Act
        CreateUserCommand.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task Handle_EmailCheckFails_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserCommand.Request("Alice", "alice@example.com");
        var expectedError = Error.New("Database connection failed");

        _userRepository
            .ExistsByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<bool>(expectedError)));

        // Act
        CreateUserCommand.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldBe("Database connection failed");
    }

    [Fact]
    public async Task Handle_CreateFails_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserCommand.Request("Alice", "alice@example.com");
        var expectedError = Error.New("Failed to create user");

        _userRepository
            .ExistsByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(false)));

        _userRepository
            .CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<User>(expectedError)));

        // Act
        CreateUserCommand.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldBe("Failed to create user");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var request = new CreateUserCommand.Request("Bob", "bob@example.com");

        _userRepository
            .ExistsByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(false)));

        _userRepository
            .CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var user = callInfo.Arg<User>();
                return Task.FromResult(Fin.Succ(user));
            });

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).ExistsByEmailAsync("bob@example.com", Arg.Any<CancellationToken>());
        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Name == "Bob" && u.Email == "bob@example.com"),
            Arg.Any<CancellationToken>());
    }
}
