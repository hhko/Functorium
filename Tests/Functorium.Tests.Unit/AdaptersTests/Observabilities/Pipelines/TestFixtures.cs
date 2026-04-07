using FluentValidation;
using Functorium.Abstractions.Errors;
using Functorium.Abstractions.Observabilities;
using Functorium.Applications.Observabilities;
using Functorium.Applications.Usecases;
using LanguageExt.Common;
using Mediator;
using Serilog.Context;

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

    /// <summary>
    /// CtxEnricher 런타임 구조 테스트를 위한 핸드라이트 Enricher.
    /// 소스 제너레이터가 생성하는 6가지 ctx 필드 패턴을 모두 포함합니다:
    /// Root 스칼라/컬렉션 + Usecase Request 스칼라/컬렉션 + Usecase Response 스칼라/컬렉션
    /// </summary>
    internal sealed class TestCommandCtxEnricher
        : IUsecaseCtxEnricher<TestCommandRequest, TestResponse>
    {
        public IDisposable? EnrichRequest(TestCommandRequest request)
        {
            var disposables = new List<IDisposable>(4);
            // Pattern 1: Root scalar (simulates [CtxRoot] interface)
            disposables.Add(LogContext.PushProperty("ctx.customer_id", "CUST-001"));
            // Pattern 2: Root collection (simulates [CtxRoot] interface + collection)
            disposables.Add(LogContext.PushProperty("ctx.items_count", 3));
            // Pattern 3: Usecase request scalar
            disposables.Add(LogContext.PushProperty("ctx.test_fixtures.request.name", request.Name));
            // Pattern 4: Usecase request collection
            disposables.Add(LogContext.PushProperty("ctx.test_fixtures.request.lines_count", 2));
            return new CompositeDisposable(disposables);
        }

        public IDisposable? EnrichResponse(TestCommandRequest request, TestResponse response)
        {
            var disposables = new List<IDisposable>(4);
            // Pattern 1: Root scalar
            disposables.Add(LogContext.PushProperty("ctx.customer_id", "CUST-001"));
            // Pattern 2: Root collection
            disposables.Add(LogContext.PushProperty("ctx.items_count", 3));
            // Pattern 5: Usecase response scalar
            disposables.Add(LogContext.PushProperty("ctx.test_fixtures.response.id", response.Id));
            // Pattern 6: Usecase response collection
            disposables.Add(LogContext.PushProperty("ctx.test_fixtures.response.tags_count", 5));
            return new CompositeDisposable(disposables);
        }

        private sealed class CompositeDisposable(List<IDisposable> disposables) : IDisposable
        {
            public void Dispose()
            {
                for (int i = disposables.Count - 1; i >= 0; i--)
                    disposables[i].Dispose();
            }
        }
    }

    /// <summary>
    /// 3-Pillar (Logging + Tracing + MetricsTag) 전파를 테스트하기 위한 Enricher.
    /// CtxEnricherContext.Push를 사용하여 모든 Pillar에 동시 전파합니다.
    /// </summary>
    internal sealed class TestCommandCtx3PillarEnricher
        : IUsecaseCtxEnricher<TestCommandRequest, TestResponse>
    {
        public IDisposable? EnrichRequest(TestCommandRequest request)
        {
            var disposables = new List<IDisposable>(3);
            // Default (Logging + Tracing)
            disposables.Add(CtxEnricherContext.Push("ctx.customer_id", "CUST-001"));
            // All (Logging + Tracing + MetricsTag)
            disposables.Add(CtxEnricherContext.Push("ctx.is_express", true, CtxPillar.All));
            // MetricsValue (수치 기록)
            disposables.Add(CtxEnricherContext.Push("ctx.item_count", 5, CtxPillar.Default | CtxPillar.MetricsValue));
            return new CompositeDisposable(disposables);
        }

        public IDisposable? EnrichResponse(TestCommandRequest request, TestResponse response)
        {
            var disposables = new List<IDisposable>(2);
            disposables.Add(CtxEnricherContext.Push("ctx.customer_id", "CUST-001"));
            disposables.Add(CtxEnricherContext.Push("ctx.is_express", true, CtxPillar.All));
            return new CompositeDisposable(disposables);
        }

        private sealed class CompositeDisposable(List<IDisposable> disposables) : IDisposable
        {
            public void Dispose()
            {
                for (int i = disposables.Count - 1; i >= 0; i--)
                    disposables[i].Dispose();
            }
        }
    }
}
