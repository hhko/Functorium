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

    private static User CreateTestUser(string name = "Alice", string email = "alice@example.com")
    {
        var userName = UserName.Create(name).IfFail(_ => throw new Exception());
        var userEmail = UserEmail.Create(email).IfFail(_ => throw new Exception());
        return User.Create(userName, userEmail, DateTime.UtcNow)
            .IfFail(_ => throw new Exception());
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsSuccessResponse()
    {
        // Arrange
        var expectedUser = CreateTestUser();
        var request = new GetUserByIdQuery.Request(expectedUser.Id.ToString());

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<User?>(expectedUser)));

        // Act
        FinResponse<GetUserByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.UserId.ShouldBe(expectedUser.Id.ToString());
                response.Name.ShouldBe("Alice");
                response.Email.ShouldBe("alice@example.com");
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_NonExistingUser_ReturnsFailure()
    {
        // Arrange
        var userId = UserId.New();
        var request = new GetUserByIdQuery.Request(userId.ToString());

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
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
        var userId = UserId.New();
        var request = new GetUserByIdQuery.Request(userId.ToString());
        var expectedError = Error.New("Database connection failed");

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
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
    public async Task Handle_InvalidUserId_ReturnsFailure()
    {
        // Arrange
        var request = new GetUserByIdQuery.Request("invalid-id");

        // Act
        FinResponse<GetUserByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("Invalid UserId format"));
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var user = CreateTestUser("Test", "test@example.com");
        var request = new GetUserByIdQuery.Request(user.Id.ToString());

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<User?>(user)));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }
}
