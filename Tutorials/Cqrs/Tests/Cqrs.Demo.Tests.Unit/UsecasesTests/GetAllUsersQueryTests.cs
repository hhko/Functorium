using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Cqrs.Demo.Tests.Unit.UsecasesTests;

public sealed class GetAllUsersQueryTests
{
    private readonly ILogger<GetAllUsersQuery.Usecase> _logger;
    private readonly IUserRepository _userRepository;
    private readonly GetAllUsersQuery.Usecase _sut;

    public GetAllUsersQueryTests()
    {
        _logger = Substitute.For<ILogger<GetAllUsersQuery.Usecase>>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new GetAllUsersQuery.Usecase(_logger, _userRepository);
    }

    [Fact]
    public async Task Handle_UsersExist_ReturnsAllUsers()
    {
        // Arrange
        var request = new GetAllUsersQuery.Request();
        var users = Seq(
            new User(Guid.NewGuid(), "Alice", "alice@example.com", DateTime.UtcNow),
            new User(Guid.NewGuid(), "Bob", "bob@example.com", DateTime.UtcNow)
        );

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(users)));

        // Act
        IFinResponse<GetAllUsersQuery.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Users.Count.ShouldBe(2);
        result.Value.Users.Any(u => u.Name == "Alice").ShouldBeTrue();
        result.Value.Users.Any(u => u.Name == "Bob").ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NoUsers_ReturnsEmptyList()
    {
        // Arrange
        var request = new GetAllUsersQuery.Request();
        var emptyUsers = Seq<User>();

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(emptyUsers)));

        // Act
        IFinResponse<GetAllUsersQuery.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Users.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_RepositoryFails_ReturnsFailure()
    {
        // Arrange
        var request = new GetAllUsersQuery.Request();
        var expectedError = Error.New("Database connection failed");

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Seq<User>>(expectedError)));

        // Act
        IFinResponse<GetAllUsersQuery.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldBe("Database connection failed");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepository()
    {
        // Arrange
        var request = new GetAllUsersQuery.Request();
        var users = Seq<User>();

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(users)));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsUserToDto_Correctly()
    {
        // Arrange
        var request = new GetAllUsersQuery.Request();
        var userId = Guid.NewGuid();
        var users = Seq(
            new User(userId, "TestUser", "test@example.com", DateTime.UtcNow)
        );

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(users)));

        // Act
        IFinResponse<GetAllUsersQuery.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.Users.Count.ShouldBe(1);
        var userDto = result.Value.Users[0];
        userDto.UserId.ShouldBe(userId);
        userDto.Name.ShouldBe("TestUser");
        userDto.Email.ShouldBe("test@example.com");
    }
}
