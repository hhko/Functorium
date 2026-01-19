using Functorium.Abstractions.Errors;
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;

using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

/// <summary>
/// 테스트용 엔티티
/// </summary>
public sealed record TestEntity(Guid Id, string Name);

/// <summary>
/// 테스트용 Adapter 인터페이스
/// </summary>
public interface ITestObservabilityAdapter : IAdapter
{
    FinT<IO, Guid> GetById(Guid id);
    FinT<IO, LanguageExt.Unit> Save(TestEntity entity);
}

/// <summary>
/// Observability 테스트용 Adapter.
/// [GeneratePipeline] 속성으로 파이프라인 클래스가 자동 생성됩니다.
/// </summary>
[GeneratePipeline]
public class TestObservabilityAdapter : ITestObservabilityAdapter
{
    public string RequestCategory => "Repository";

    /// <summary>
    /// GetById 메서드의 커스텀 핸들러.
    /// null이면 기본 성공 응답 반환.
    /// </summary>
    public Func<Guid, Fin<Guid>>? GetByIdHandler { get; set; }

    /// <summary>
    /// Save 메서드의 커스텀 핸들러.
    /// null이면 기본 성공 응답 반환.
    /// </summary>
    public Func<TestEntity, Fin<LanguageExt.Unit>>? SaveHandler { get; set; }

    /// <summary>
    /// 기본 생성자: 성공 응답 반환
    /// </summary>
    public TestObservabilityAdapter()
    {
    }

    public virtual FinT<IO, Guid> GetById(Guid id)
    {
        return IO.lift(() =>
        {
            if (GetByIdHandler != null)
            {
                return GetByIdHandler(id);
            }
            return Fin.Succ(id);
        });
    }

    public virtual FinT<IO, LanguageExt.Unit> Save(TestEntity entity)
    {
        return IO.lift(() =>
        {
            if (SaveHandler != null)
            {
                return SaveHandler(entity);
            }
            return Fin.Succ(LanguageExt.Unit.Default);
        });
    }
}

/// <summary>
/// 테스트용 Error 팩토리 메서드
/// </summary>
public static class TestErrors
{
    public static Error CreateExpectedError() =>
        new ErrorCodeExpected("Test.NotFound", "testValue", "Entity not found");

    public static Error CreateExpectedErrorT() =>
        new ErrorCodeExpected<int>("Test.InvalidValue", 42, "Invalid value provided");

    public static Error CreateExceptionalError() =>
        new ErrorCodeExceptional("Test.DatabaseError", new InvalidOperationException("Database connection failed"));

    public static Error CreateAggregateError() =>
        Error.Many(
            new ErrorCodeExpected("Test.Error1", "value1", "First error"),
            new ErrorCodeExpected("Test.Error2", "value2", "Second error"));
}
