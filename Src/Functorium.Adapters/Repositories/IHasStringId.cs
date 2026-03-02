namespace Functorium.Adapters.Repositories;

/// <summary>
/// string Id 프로퍼티를 가진 EF Core 모델의 공통 인터페이스.
/// EfCoreRepositoryBase에서 ByIdPredicate/ByIdsPredicate 기본 구현을 제공하기 위해 사용합니다.
/// </summary>
public interface IHasStringId
{
    string Id { get; set; }
}
