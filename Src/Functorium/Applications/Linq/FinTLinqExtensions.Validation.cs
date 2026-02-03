using LanguageExt.Traits;

namespace Functorium.Applications.Linq;

/// <summary>
/// Validation&lt;Error, A&gt; 타입을 위한 LINQ 확장 메서드
/// </summary>
public static partial class FinTLinqExtensions
{
    // =========================================================================
    // Validation → FinT SelectMany (제네릭 모나드)
    // =========================================================================

    /// <summary>
    /// Validation → FinT 변환: Validation을 FinT로 승격하고 체이닝하는 SelectMany
    ///
    /// LanguageExt 패턴 적용:
    ///   Validation&lt;Error, A&gt; → FinT&lt;M, A&gt; → FinT&lt;M, B&gt; → FinT&lt;M, C&gt;
    ///
    /// LINQ 쿼리:
    ///   from validVal in validation          // Validation&lt;Error, A&gt;
    ///   from finTVal in finTSelector(val)    // FinT&lt;M, B&gt;
    ///   select result                        // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Unit&gt; result =
    ///       from identifier in LimitSampleIdentifier.Validate(...)  // Validation&lt;Error, LimitSampleIdentifier&gt;
    ///       from saved in SaveToDatabase(identifier)                 // FinT&lt;IO, Unit&gt;
    ///       select saved;
    ///
    /// 주의:
    ///   - 검증 실패 시 첫 번째 에러만 사용 (errors.Head)
    ///   - 모든 에러를 처리해야 하는 경우 별도 구현 필요
    /// </summary>
    public static FinT<M, C> SelectMany<M, A, B, C>(
        this Validation<Error, A> validation,
        Func<A, FinT<M, B>> finTSelector,
        Func<A, B, C> projector)
        where M : Monad<M>
    {
        Fin<A> fin = validation.Match(
            Succ: value => Fin.Succ(value),
            Fail: errors => Fin.Fail<A>(errors.Head));

        return FinT.lift<M, A>(fin).SelectMany(finTSelector, projector);
    }

    /// <summary>
    /// Validation → FinT 변환 (단순 Map): Validation을 FinT로 승격하고 변환
    ///
    /// LINQ 쿼리:
    ///   from val in validation  // Validation&lt;Error, A&gt;
    ///   select result          // B
    ///
    /// 사용 예:
    ///   FinT&lt;IO, LimitSampleIdentifier&gt; result =
    ///       from identifier in LimitSampleIdentifier.Validate(lineId, processId, partId, version)
    ///       select identifier;
    /// </summary>
    public static FinT<M, B> SelectMany<M, A, B>(
        this Validation<Error, A> validation,
        Func<A, B> selector)
        where M : Monad<M>
    {
        Fin<A> fin = validation.Match(
            Succ: value => Fin.Succ(value),
            Fail: errors => Fin.Fail<A>(errors.Head));

        return FinT.lift<M, A>(fin).Map(selector);
    }

    // =========================================================================
    // Validation → FinT<IO, A> SelectMany (IO 모나드 특화)
    // =========================================================================

    /// <summary>
    /// Validation → FinT&lt;IO, B&gt; 변환: IO 모나드에 특화된 SelectMany
    ///
    /// LINQ 쿼리 표현식에서 Validation을 직접 사용 가능하게 함:
    ///   from validated in LimitSampleIdentifier.Validate(...)  // Validation&lt;Error, A&gt; → FinT&lt;IO, A&gt;
    ///   from result in DoSomething(validated)                  // FinT&lt;IO, B&gt;
    ///   select result
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Unit&gt; usecase =
    ///       from identifier in LimitSampleIdentifier.Validate(lineId, processId, partId, version)
    ///       from saved in SaveToDatabase(identifier)
    ///       select saved;
    ///
    /// 장점:
    ///   - AsFinT() 호출 불필요
    ///   - 타입 추론 자동
    ///   - LINQ 쿼리 표현식에서 자연스럽게 사용
    /// </summary>
    public static FinT<IO, C> SelectMany<A, B, C>(
        this Validation<Error, A> validation,
        Func<A, FinT<IO, B>> finTSelector,
        Func<A, B, C> projector)
    {
        Fin<A> fin = validation.Match(
            Succ: value => Fin.Succ(value),
            Fail: errors => Fin.Fail<A>(errors.Head));

        return FinT.lift<IO, A>(fin).SelectMany(finTSelector, projector);
    }

    // =========================================================================
    // FinT → Validation SelectMany (제네릭 모나드)
    // =========================================================================

    /// <summary>
    /// FinT → Validation 체이닝: FinT 컨텍스트에서 Validation 결과를 체이닝하는 SelectMany
    ///
    /// Validation을 Fin으로 변환 후 FinT로 승격:
    ///   FinT&lt;M, A&gt; → Validation&lt;Error, B&gt; → FinT&lt;M, C&gt;
    ///
    /// LINQ 쿼리:
    ///   from finTVal in finTValue                    // FinT&lt;M, A&gt;
    ///   from validated in validationSelector(val)   // Validation&lt;Error, B&gt;
    ///   select result                               // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Response&gt; result =
    ///       from request in GetRequest()                           // FinT&lt;IO, Request&gt;
    ///       from identifier in Identifier.Validate(request.Id)     // Validation&lt;Error, Identifier&gt;
    ///       from saved in repository.Save(identifier)              // FinT&lt;IO, Entity&gt;
    ///       select new Response(saved.Id);
    ///
    /// 주요 사용 시나리오:
    ///   - Value Object 검증을 FinT 체인 중간에 포함
    ///   - 도메인 규칙 검증 결과를 체인에 포함
    ///   - 복합 검증 로직을 단계별로 수행
    ///
    /// 주의:
    ///   - 검증 실패 시 첫 번째 에러만 사용 (errors.Head)
    ///   - 모든 에러를 처리해야 하는 경우 별도 구현 필요
    /// </summary>
    public static FinT<M, C> SelectMany<M, A, B, C>(
        this FinT<M, A> finT,
        Func<A, Validation<Error, B>> validationSelector,
        Func<A, B, C> projector)
        where M : Monad<M>
    {
        return finT.Bind(a =>
        {
            Fin<B> fin = validationSelector(a).Match(
                Succ: value => Fin.Succ(value),
                Fail: errors => Fin.Fail<B>(errors.Head));

            return FinT.lift<M, B>(fin).Map(b => projector(a, b));
        });
    }

    /// <summary>
    /// FinT → Validation 체이닝 (IO 특화): IO 모나드에 특화된 SelectMany
    ///
    /// LINQ 쿼리 표현식에서 타입 추론이 더 정확하게 동작:
    ///   from finTVal in finTValue                    // FinT&lt;IO, A&gt;
    ///   from validated in validationSelector(val)   // Validation&lt;Error, B&gt;
    ///   select result                               // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Response&gt; usecase =
    ///       from request in GetRequest()                           // FinT&lt;IO, Request&gt;
    ///       from identifier in Identifier.Validate(request.Id)     // Validation&lt;Error, Identifier&gt;
    ///       from saved in repository.Save(identifier)              // FinT&lt;IO, Entity&gt;
    ///       select new Response(saved.Id);
    /// </summary>
    public static FinT<IO, C> SelectMany<A, B, C>(
        this FinT<IO, A> finT,
        Func<A, Validation<Error, B>> validationSelector,
        Func<A, B, C> projector)
    {
        return finT.Bind(a =>
        {
            Fin<B> fin = validationSelector(a).Match(
                Succ: value => Fin.Succ(value),
                Fail: errors => Fin.Fail<B>(errors.Head));

            return FinT.lift<IO, B>(fin).Map(b => projector(a, b));
        });
    }
}
