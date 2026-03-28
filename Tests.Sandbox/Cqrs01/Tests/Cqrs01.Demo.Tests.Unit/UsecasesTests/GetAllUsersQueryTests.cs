using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Cqrs01.Demo.Tests.Unit.UsecasesTests;

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

    private static User CreateTestUser(string name, string email)
    {
        var userName = UserName.Create(name).IfFail(_ => throw new Exception());
        var userEmail = UserEmail.Create(email).IfFail(_ => throw new Exception());
        return User.Create(userName, userEmail, DateTime.UtcNow)
            .IfFail(_ => throw new Exception());
    }

    [Fact]
    public async Task Handle_UsersExist_ReturnsAllUsers()
    {
        // Arrange
        var request = new GetAllUsersQuery.Request();
        var users = Seq(
            CreateTestUser("Alice", "alice@example.com"),
            CreateTestUser("Bob", "bob@example.com")
        );

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(users)));

        // Act
        FinResponse<GetAllUsersQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.Users.Count.ShouldBe(2);
                response.Users.Any(u => u.Name == "Alice").ShouldBeTrue();
                response.Users.Any(u => u.Name == "Bob").ShouldBeTrue();
            },
            Fail: _ => throw new Exception("Should be success"));
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
        FinResponse<GetAllUsersQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.Users.Count.ShouldBe(0);
            },
            Fail: _ => throw new Exception("Should be success"));
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
        FinResponse<GetAllUsersQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database connection failed"));
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
        var user = CreateTestUser("TestUser", "test@example.com");
        var users = Seq(user);

        _userRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(users)));

        // Act
        FinResponse<GetAllUsersQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.Users.Count.ShouldBe(1);
                var userDto = response.Users[0];
                userDto.UserId.ShouldBe(user.Id.ToString());
                userDto.Name.ShouldBe("TestUser");
                userDto.Email.ShouldBe("test@example.com");
            },
            Fail: _ => throw new Exception("Should be success"));
    }
}
