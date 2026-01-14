using FluentValidation;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Cqrs;
using LanguageExt.Common;
using Mediator;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// Pipeline 테스트를 위한 공통 Test Fixtures
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// 테스트용 Command Request
    /// </summary>
    public sealed record class TestCommandRequest(string Name) : ICommandRequest<TestSuccessResponse>;

    /// <summary>
    /// 테스트용 Query Request
    /// </summary>
    public sealed record class TestQueryRequest(Guid Id) : IQueryRequest<TestSuccessResponse>;

    /// <summary>
    /// 테스트용 성공 Response
    /// </summary>
    public sealed record class TestSuccessResponse(Guid Id, string Name);

    /// <summary>
    /// CRTP 패턴을 따르는 테스트용 Response.
    /// IFinResponse와 IFinResponseFactory를 직접 구현합니다.
    /// </summary>
    public sealed record class TestResponse : IFinResponse<TestResponse>, IFinResponseFactory<TestResponse>, IFinResponseWithError
    {
        public bool IsSucc { get; init; }
        public bool IsFail => !IsSucc;
        public Guid Id { get; init; }
        public string? Name { get; init; }

        // IFinResponseWithError 구현 (non-nullable)
        public Error Error { get; init; } = Error.New("No error");

        private TestResponse() { }

        public static TestResponse CreateSuccess(Guid id, string name = "Test")
            => new() { IsSucc = true, Id = id, Name = name };

        public static TestResponse CreateFail(Error error)
            => new() { IsSucc = false, Error = error };

        // IFinResponseFactory<TestResponse> 구현
        static TestResponse IFinResponseFactory<TestResponse>.CreateFail(Error error)
            => CreateFail(error);
    }

    /// <summary>
    /// 테스트용 간단한 Request (IMessage만 구현)
    /// </summary>
    public sealed record class SimpleTestRequest(string Name) : IMessage;

    /// <summary>
    /// 테스트용 Validator
    /// </summary>
    public class TestCommandRequestValidator : FluentValidation.AbstractValidator<TestCommandRequest>
    {
        public TestCommandRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required");

            RuleFor(x => x.Name)
                .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters");
        }
    }
}
