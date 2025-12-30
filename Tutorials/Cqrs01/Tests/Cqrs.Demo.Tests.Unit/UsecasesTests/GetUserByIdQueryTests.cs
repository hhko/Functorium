using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Cqrs01.Demo.Tests.Unit.UsecasesTests;

public sealed class GetUserByIdQueryTests
{
    private readonly ILogger<GetUserByIdQuery.Usecase> _logger;
    private readonly IUserRepository _userRepository;
    private readonly GetUserByIdQuery.Usecase _sut;

    public GetUserByIdQueryTests()
    {
        _logger = Substitute.For<ILogger<GetUserByIdQuery.Usecase>>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new GetUserByIdQuery.Usecase(_logger, _userRepository);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User(userId, "Alice", "alice@example.com", DateTime.UtcNow);
        var request = new GetUserByIdQuery.Request(userId);

        _userRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<User?>(expectedUser)));

        // Act
        FinResponse<GetUserByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.UserId.ShouldBe(userId);
                response.Name.ShouldBe("Alice");
                response.Email.ShouldBe("alice@example.com");
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_NonExistingUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new GetUserByIdQuery.Request(userId);

        _userRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<User?>(null)));

        // Act
        FinResponse<GetUserByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task Handle_RepositoryFails_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new GetUserByIdQuery.Request(userId);
        var expectedError = Error.New("Database connection failed");

        _userRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<User?>(expectedError)));

        // Act
        FinResponse<GetUserByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database connection failed"));
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new GetUserByIdQuery.Request(userId);
        var user = new User(userId, "Test", "test@example.com", DateTime.UtcNow);

        _userRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<User?>(user)));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
    }
}
