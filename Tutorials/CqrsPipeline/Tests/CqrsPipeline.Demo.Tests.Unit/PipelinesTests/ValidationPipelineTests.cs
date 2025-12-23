using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace CqrsPipeline.Demo.Tests.Unit.PipelinesTests;

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
    public sealed record class TestRequest(string Name, decimal Price) : ICommandRequest<TestResponse>;

    /// <summary>
    /// 테스트용 Response
    /// </summary>
    public sealed record class TestResponse(Guid Id) : IResponse;

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
        var pipeline = new UsecaseValidationPipeline<TestRequest, IFinResponse<TestResponse>>(validators);
        var request = new TestRequest("Valid Name", 100m);
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid())));

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(expectedResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldBe(expectedResponse.Value);
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesThrough()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, IFinResponse<TestResponse>>(validators);
        var request = new TestRequest("Valid Name", 100m);
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid())));

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(expectedResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldBe(expectedResponse.Value);
    }

    [Fact]
    public async Task Handle_InvalidRequest_SingleError_ReturnsFailure()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, IFinResponse<TestResponse>>(validators);
        var request = new TestRequest("Valid Name", -100m); // Invalid price

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => throw new InvalidOperationException("Should not reach handler");

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldContain("Price");
    }

    [Fact]
    public async Task Handle_InvalidRequest_MultipleErrors_ReturnsManyErrors()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, IFinResponse<TestResponse>>(validators);
        var request = new TestRequest("", -100m); // Both invalid

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => throw new InvalidOperationException("Should not reach handler");

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        // ManyErrors는 여러 에러를 포함
        result.Error.ShouldBeOfType<ManyErrors>();
    }

    [Fact]
    public async Task Handle_InvalidRequest_DoesNotCallHandler()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestRequest, IFinResponse<TestResponse>>(validators);
        var request = new TestRequest("", -100m); // Both invalid
        var handlerCalled = false;

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) =>
            {
                handlerCalled = true;
                return ValueTask.FromResult<IFinResponse<TestResponse>>(
                    new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid()))));
            };

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        handlerCalled.ShouldBeFalse();
    }
}
