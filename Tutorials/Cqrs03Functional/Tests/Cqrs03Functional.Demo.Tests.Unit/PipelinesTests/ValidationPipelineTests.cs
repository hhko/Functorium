using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace Cqrs03Functional.Demo.Tests.Unit.PipelinesTests;

/// <summary>
/// UsecaseValidationPipeline 테스트
/// FluentValidation을 사용한 요청 검증 파이프라인 테스트
/// </summary>
public sealed class ValidationPipelineTests
{
    #region Test Fixtures

    /// <summary>
    /// 테스트용 Request
    /// </summary>
    public sealed record class TestRequest(string Name, decimal Price) : IMessage;

    /// <summary>
    /// CRTP 패턴을 따르는 테스트용 Response.
    /// IFinResponse{TSelf}와 IFinResponseFactory{TSelf}를 직접 구현합니다.
    /// </summary>
    public sealed record class TestResponse : IFinResponse<TestResponse>, IFinResponseFactory<TestResponse>
    {
        public bool IsSucc { get; init; }
        public bool IsFail => !IsSucc;
        public Guid Id { get; init; }
        public Error? Error { get; init; }

        private TestResponse() { }

        public static TestResponse CreateSuccess(Guid id) => new() { IsSucc = true, Id = id };
        public static TestResponse CreateFail(Error error) => new() { IsSucc = false, Error = error };

        // IFinResponseFactory<TestResponse> 구현
        static TestResponse IFinResponseFactory<TestResponse>.CreateFail(Error error) => CreateFail(error);
    }

    /// <summary>
    /// 테스트용 Validator
    /// </summary>
    public sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
        }
    }

    #endregion

    [Fact]
    public async Task Handle_NoValidators_PassesThrough()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var pipeline = new UsecaseValidationPipeline<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Valid Name", 100m);
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesThrough()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Valid Name", 100m);
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_InvalidRequest_SingleError_ReturnsFailure()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Valid Name", -100m); // Invalid price

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => throw new InvalidOperationException("Should not reach handler");

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.Message.ShouldContain("Price");
    }

    [Fact]
    public async Task Handle_InvalidRequest_MultipleErrors_ReturnsManyErrors()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", -100m); // Both invalid

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => throw new InvalidOperationException("Should not reach handler");

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        // ManyErrors는 여러 에러를 포함
        result.Error.ShouldBeOfType<ManyErrors>();
    }

    [Fact]
    public async Task Handle_InvalidRequest_DoesNotCallHandler()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", -100m); // Both invalid
        var handlerCalled = false;

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) =>
            {
                handlerCalled = true;
                return ValueTask.FromResult(TestResponse.CreateSuccess(Guid.NewGuid()));
            };

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        handlerCalled.ShouldBeFalse();
    }
}
