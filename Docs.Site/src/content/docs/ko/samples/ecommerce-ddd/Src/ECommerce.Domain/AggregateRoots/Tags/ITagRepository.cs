using Functorium.Domains.Repositories;

namespace ECommerce.Domain.AggregateRoots.Tags;

/// <summary>
/// 태그 리포지토리 인터페이스
/// </summary>
public interface ITagRepository : IRepository<Tag, TagId>;
