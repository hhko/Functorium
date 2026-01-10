using FluentValidation;
using Functorium.Adapters.Observabilities.Pipelines;
using LanguageExt.Common;
using Mediator;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseValidationPipeline 테스트
/// FluentValidation을 사용한 요청 검증 파이프라인 테스트
/// </summary>
public sealed class UsecaseValidationPipelineTests
{
    [Fact]
    public async Task Handle_NoValidators_PassesThrough()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommandRequest>>();
        var pipeline = new UsecaseValidationPipeline<TestCommandRequest, TestResponse>(validators);
        var request = new TestCommandRequest("ValidName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
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
        var validators = new List<IValidator<TestCommandRequest>> { new TestCommandRequestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestCommandRequest, TestResponse>(validators);
        var request = new TestCommandRequest("ValidName"); // 3자 이상
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_InvalidRequest_EmptyName_ReturnsFailure()
    {
        // Arrange
        var validators = new List<IValidator<TestCommandRequest>> { new TestCommandRequestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestCommandRequest, TestResponse>(validators);
        var request = new TestCommandRequest(""); // 빈 이름

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => throw new InvalidOperationException("Should not reach handler");

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.Message.ShouldContain("Name");
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShortName_ReturnsFailure()
    {
        // Arrange
        var validators = new List<IValidator<TestCommandRequest>> { new TestCommandRequestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestCommandRequest, TestResponse>(validators);
        var request = new TestCommandRequest("AB"); // 3자 미만

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => throw new InvalidOperationException("Should not reach handler");

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.Message.ShouldContain("3 characters");
    }

    [Fact]
    public async Task Handle_InvalidRequest_DoesNotCallHandler()
    {
        // Arrange
        var validators = new List<IValidator<TestCommandRequest>> { new TestCommandRequestValidator() };
        var pipeline = new UsecaseValidationPipeline<TestCommandRequest, TestResponse>(validators);
        var request = new TestCommandRequest(""); // Invalid
        var handlerCalled = false;

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
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

    [Fact]
    public async Task Handle_MultipleValidators_AllExecuted()
    {
        // Arrange
        var secondValidator = new SecondTestCommandRequestValidator();
        var validators = new List<IValidator<TestCommandRequest>>
        {
            new TestCommandRequestValidator(),
            secondValidator
        };
        var pipeline = new UsecaseValidationPipeline<TestCommandRequest, TestResponse>(validators);
        var request = new TestCommandRequest("ValidName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    /// <summary>
    /// 추가 테스트용 Validator
    /// </summary>
    private sealed class SecondTestCommandRequestValidator : AbstractValidator<TestCommandRequest>
    {
        public SecondTestCommandRequestValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters");
        }
    }
}
