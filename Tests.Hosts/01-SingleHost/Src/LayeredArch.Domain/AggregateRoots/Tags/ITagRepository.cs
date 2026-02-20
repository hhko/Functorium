using Functorium.Domains.Repositories;

namespace LayeredArch.Domain.AggregateRoots.Tags;

/// <summary>
/// 태그 리포지토리 인터페이스
/// </summary>
public interface ITagRepository : IRepository<Tag, TagId>;
