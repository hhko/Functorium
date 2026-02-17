namespace Functorium.Domains.Entities;

/// <summary>
/// 낙관적 동시성 제어를 위한 믹스인 인터페이스.
/// EF Core의 [Timestamp]/IsRowVersion()과 매핑됩니다.
/// 고경합 Aggregate Root에 선택적으로 적용합니다.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// 낙관적 동시성 제어를 위한 행 버전.
    /// </summary>
    byte[] RowVersion { get; }
}
