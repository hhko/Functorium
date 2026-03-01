using Dapper;
using Functorium.Domains.Specifications;
namespace Functorium.Adapters.Repositories;

/// <summary>
/// Specification → SQL WHERE 절 번역기 레지스트리.
/// 엔티티 타입별로 한 번 구성하면 여러 Dapper 어댑터가 테이블 별칭만 달리하여 공유할 수 있습니다.
/// </summary>
public sealed class DapperSpecTranslator<TEntity>
{
    private Func<string, (string Where, DynamicParameters Params)>? _allHandler;
    private readonly Dictionary<Type, Func<Specification<TEntity>, string, (string Where, DynamicParameters Params)>> _handlers = new();

    /// <summary>
    /// IsAll (항등원) Specification에 대한 핸들러를 등록합니다.
    /// 테이블 별칭 문자열을 인자로 받습니다.
    /// </summary>
    public DapperSpecTranslator<TEntity> WhenAll(
        Func<string, (string Where, DynamicParameters Params)> handler)
    {
        _allHandler = handler;
        return this;
    }

    /// <summary>
    /// 특정 Specification 타입에 대한 SQL 번역 핸들러를 등록합니다.
    /// </summary>
    public DapperSpecTranslator<TEntity> When<TSpec>(
        Func<TSpec, string, (string Where, DynamicParameters Params)> handler)
        where TSpec : Specification<TEntity>
    {
        _handlers[typeof(TSpec)] = (spec, alias) => handler((TSpec)spec, alias);
        return this;
    }

    /// <summary>
    /// Specification을 SQL WHERE 절로 번역합니다.
    /// </summary>
    /// <param name="spec">번역할 Specification</param>
    /// <param name="tableAlias">테이블 별칭 (예: "p"). 빈 문자열이면 별칭 없이 컬럼명만 사용.</param>
    public (string Where, DynamicParameters Params) Translate(
        Specification<TEntity> spec, string tableAlias = "")
    {
        if (spec.IsAll)
            return _allHandler?.Invoke(tableAlias) ?? ("", new DynamicParameters());

        if (_handlers.TryGetValue(spec.GetType(), out var handler))
            return handler(spec, tableAlias);

        throw new NotSupportedException(
            $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.");
    }

    /// <summary>
    /// DynamicParameters 생성 헬퍼.
    /// </summary>
    public static DynamicParameters Params(params (string Name, object Value)[] values)
    {
        var p = new DynamicParameters();
        foreach (var (name, value) in values)
            p.Add(name, value);
        return p;
    }

    /// <summary>
    /// 테이블 별칭이 있으면 "alias." 접두사를, 없으면 빈 문자열을 반환합니다.
    /// WHERE 절 작성 시 컬럼 접두사로 사용합니다.
    /// </summary>
    public static string Prefix(string tableAlias)
        => string.IsNullOrEmpty(tableAlias) ? "" : $"{tableAlias}.";
}
